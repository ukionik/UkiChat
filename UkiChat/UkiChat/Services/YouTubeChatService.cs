using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UkiChat.Configuration;
using UkiChat.Diagnostics;
using UkiChat.Model.Chat;
using UkiChat.Model.YouTube;

namespace UkiChat.Services;

public class YouTubeChatService : IYouTubeChatService
{
    private readonly YouTubeChatClient _chatClient;
    private readonly IDatabaseContext _databaseContext;
    private readonly ILocalizationService _localizationService;
    private readonly Lock _reconnectLock = new();
    private readonly ISignalRService _signalRService;
    private string _channelName = "";

    // true — разрыв инициирован намеренно (смена канала / явное отключение), переподключаться не нужно
    private volatile bool _intentionalDisconnect;
    private CancellationTokenSource? _reconnectCts;

    public YouTubeChatService(IDatabaseContext databaseContext
        , ISignalRService signalRService
        , ILocalizationService localizationService
        , ILogger<YouTubeChatClient> chatClientLogger)
    {
        _databaseContext = databaseContext;
        _signalRService = signalRService;
        _localizationService = localizationService;
        _chatClient = new YouTubeChatClient(chatClientLogger);

        _chatClient.MessageReceived += async (_, e) =>
        {
            if (e.Message == null) return;
            var mentionNicks = _databaseContext.AppSettingsRepository.GetActiveAppSettings().MentionNicknames;
            await signalRService.SendChatMessageAsync(
                UkiChatMessage.FromYouTubeMessage(e.Message).WithMentionCheck(mentionNicks));
        };

        _chatClient.MessageDeleted += async (_, messageId) =>
        {
            StartupDiagnostics.Log("yt-chat", $"Message deleted: {messageId}");
            await signalRService.SendMessageDeletedAsync(messageId);
        };

        _chatClient.Connected += async (_, _) =>
        {
            StartupDiagnostics.Log("yt-chat", "Connected");
            await SendChatMessageNotification(string.Format(
                localizationService.GetString("youtube.connectedToChannel"), _channelName));
        };

        _chatClient.Disconnected += async (_, e) =>
        {
            StartupDiagnostics.Log("yt-chat", $"Disconnected: reason={e.Reason}");

            if (!_intentionalDisconnect)
                StartReconnectLoop();
            await Task.CompletedTask;
        };

        _chatClient.Error += (_, e) => { StartupDiagnostics.LogError("yt-chat", $"Error: {e.Message}"); };
    }

    public async Task ConnectAsync(YouTubeConnectionParams connectionParams)
    {
        using var _ = StartupDiagnostics.Measure("yt-chat", $"ConnectAsync(channel={connectionParams.ChannelName})");
        if (string.IsNullOrEmpty(connectionParams.ChannelName))
        {
            StartupDiagnostics.Log("yt-chat", "Channel not configured, aborting connect");
            return;
        }

        CancelReconnectLoop();
        _channelName = connectionParams.ChannelName;

        try
        {
            await SendChatMessageNotification(string.Format(
                _localizationService.GetString("youtube.connectingToChannel"), connectionParams.ChannelName));

            using (StartupDiagnostics.Measure("yt-chat", "  _chatClient.ConnectByChannelAsync (InnerTube)"))
            {
                await _chatClient.ConnectByChannelAsync(connectionParams.ChannelName);
            }
            _intentionalDisconnect = false;
        }
        catch (Exception ex)
        {
            // Канал может быть просто не в эфире — это нормальная ситуация, продолжаем ждать в фоне
            StartupDiagnostics.Log("yt-chat", $"Connection failed: {ex.Message}");
            await SendChatMessageNotification(string.Format(
                _localizationService.GetString("youtube.connectingToChannelError"), connectionParams.ChannelName));
            _intentionalDisconnect = false;
            StartReconnectLoop();
        }
    }

    public async Task ChangeChannelAsync(string newChannel)
    {
        var youTubeSettings = _databaseContext.YouTubeSettingsRepository.GetActiveSettings();
        var oldChannel = youTubeSettings.Channel ?? "";
        if (oldChannel == newChannel)
            return;

        // Останавливаем переподключение к старому каналу
        _intentionalDisconnect = true;
        CancelReconnectLoop();

        youTubeSettings.Channel = newChannel;
        _databaseContext.YouTubeSettingsRepository.Save(youTubeSettings);

        if (newChannel.Length == 0)
        {
            _channelName = "";
            await _chatClient.DisconnectAsync();
            return;
        }

        await ConnectAsync(new YouTubeConnectionParams(oldChannel, newChannel));
    }

    public Task LoadGlobalDataAsync()
    {
        return Task.CompletedTask;
    }

    public Task LoadChannelDataAsync()
    {
        return Task.CompletedTask;
    }

    private void StartReconnectLoop()
    {
        lock (_reconnectLock)
        {
            if (_reconnectCts is { IsCancellationRequested: false })
                return;

            _reconnectCts?.Dispose();
            var cts = new CancellationTokenSource();
            _reconnectCts = cts;
            _ = Task.Run(() => ReconnectLoopAsync(cts));
        }
    }

    private void CancelReconnectLoop()
    {
        lock (_reconnectLock)
        {
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _reconnectCts = null;
        }
    }

    /// <summary>
    ///     Цикл переподключения с интервалом 15с. На каждой попытке заново резолвит активную
    ///     трансляцию канала (videoId меняется при рестарте стрима).
    /// </summary>
    private async Task ReconnectLoopAsync(CancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested) return;
                if (string.IsNullOrEmpty(_channelName)) return;

                try
                {
                    await _chatClient.ConnectByChannelAsync(_channelName, cancellationToken);
                    StartupDiagnostics.Log("yt-chat", "Переподключение успешно");
                    return;
                }
                // Только НАШ токен (отмена реконнекта). Таймаут HttpClient внутри ConnectByChannelAsync
                // тоже бросает TaskCanceledException : OperationCanceledException — ловится по типу,
                // поэтому фильтруем по токену, иначе таймаут убивал бы цикл навсегда.
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Канал всё ещё не в эфире — продолжаем ждать
                    StartupDiagnostics.Log("yt-chat", $"Reconnect attempt failed: {ex.Message}");
                }
            }
        }
        finally
        {
            // Освобождаем _reconnectCts при выходе из цикла, чтобы следующий разрыв смог
            // запустить новый цикл (иначе живой неотменённый CTS навсегда блокирует StartReconnectLoop).
            lock (_reconnectLock)
            {
                if (_reconnectCts == cts)
                {
                    _reconnectCts.Dispose();
                    _reconnectCts = null;
                }
            }
        }
    }

    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromYouTubeMessageNotification(message));
    }
}
