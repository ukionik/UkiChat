using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UkiChat.Configuration;
using UkiChat.Diagnostics;
using UkiChat.Entities;
using UkiChat.Model.Chat;
using UkiChat.Model.VkVideoLive;

namespace UkiChat.Services;

public class VkVideoLiveChatService : IVkVideoLiveChatService
{
    private readonly VkVideoLiveChatClient _chatClient;
    private readonly IDatabaseContext _databaseContext;
    private readonly IDatabaseService _databaseService;
    private readonly ILocalizationService _localizationService;
    private readonly Lock _reconnectLock = new();
    private readonly ISignalRService _signalRService;
    private readonly IVkVideoLiveApiService _vkVideoLiveApiService;
    private string _channelName = "";
    private long _channelId;

    // true — разрыв инициирован намеренно (смена канала / явное отключение), переподключаться не нужно
    private volatile bool _intentionalDisconnect;
    private CancellationTokenSource? _reconnectCts;

    public VkVideoLiveChatService(IDatabaseContext databaseContext
        , IDatabaseService databaseService
        , IVkVideoLiveApiService vkVideoLiveApiService
        , ISignalRService signalRService
        , ILocalizationService localizationService
        , ILogger<VkVideoLiveChatClient> chatClientLogger)
    {
        _databaseContext = databaseContext;
        _databaseService = databaseService;
        _vkVideoLiveApiService = vkVideoLiveApiService;
        _signalRService = signalRService;
        _localizationService = localizationService;
        _chatClient = new VkVideoLiveChatClient(chatClientLogger);
        // VK Video Live events
        _chatClient.MessageReceived += async (_, e) =>
        {
            if (e.Message == null) return;

            if (e.Message.Type == "delete_message")
            {
                Console.WriteLine($"[VkVideoLive] Message deleted: {e.Message.Id}");
                await signalRService.SendMessageDeletedAsync(e.Message.Id.ToString());
                return;
            }

            if (e.Message.Type == "chat_ban" && e.Message.BannedUser != null)
            {
                var banned = e.Message.BannedUser;
                var username = !string.IsNullOrEmpty(banned.DisplayName) ? banned.DisplayName : banned.Nick;
                Console.WriteLine($"[VkVideoLive] User banned: {username}");
                await signalRService.SendUserMessagesDeletedAsync(username);
                return;
            }

            Console.WriteLine($"[VkVideoLive] Message received from: {e.Message.Data?.Author?.DisplayName}");
            var mentionNicks = _databaseContext.AppSettingsRepository.GetActiveAppSettings().MentionNicknames;
            await signalRService.SendChatMessageAsync(
                UkiChatMessage.FromVkVideoLiveMessage(e.Message).WithMentionCheck(mentionNicks));
        };

        _chatClient.Connected += async (_, _) =>
        {
            StartupDiagnostics.Log("vk-chat", "Connected");
            await SendChatMessageNotification(string.Format(
                localizationService.GetString("vkvideolive.connectedToChannel"),
                _channelName));
        };

        _chatClient.Disconnected += async (_, e) =>
        {
            StartupDiagnostics.Log("vk-chat", $"Disconnected: reason={e.Reason} channel={e.ChannelName}");
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("vkvideolive.disconnectedFromChannel"), e.ChannelName));

            if (!_intentionalDisconnect)
                StartReconnectLoop();
        };

        _chatClient.Error += (_, e) => { StartupDiagnostics.LogError("vk-chat", $"Error: {e.Message}"); };
    }

    public async Task ConnectAsync(VkVideoLiveConnectionParams connectionParams)
    {
        using var _ = StartupDiagnostics.Measure("vk-chat", $"ConnectAsync(channel={connectionParams.ChannelName})");
        if (string.IsNullOrEmpty(connectionParams.ChannelName))
        {
            StartupDiagnostics.Log("vk-chat", "Channel not configured, aborting connect");
            return;
        }

        if (connectionParams.ChannelId == 0 || string.IsNullOrEmpty(connectionParams.WsAccessToken))
        {
            StartupDiagnostics.Log("vk-chat",
                $"Connection params not configured: channelId={connectionParams.ChannelId} hasToken={!string.IsNullOrEmpty(connectionParams.WsAccessToken)}");
            return;
        }

        CancelReconnectLoop();

        try
        {
            _channelName = connectionParams.ChannelName;
            _channelId = connectionParams.ChannelId;
            await SendChatMessageNotification(string.Format(
                _localizationService.GetString("vkvideolive.connectingToChannel"), connectionParams.ChannelName));

            using (StartupDiagnostics.Measure("vk-chat", "  _chatClient.ConnectAsync (WebSocket/Centrifuge)"))
            {
                await _chatClient.ConnectAsync(connectionParams.WsAccessToken, connectionParams.ChannelId, connectionParams.ChannelName);
            }
            _intentionalDisconnect = false;
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("vk-chat", $"Connection error: {ex.Message}", ex);
            await SendChatMessageNotification(string.Format(
                _localizationService.GetString("vkvideolive.connectingToChannelError"),
                connectionParams.ChannelName));
        }
    }

    public async Task ChangeChannelAsync(string newChannel)
    {
        var vkVideoLiveSettings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
        var oldChannel = vkVideoLiveSettings.Channel ?? "";
        if (oldChannel == newChannel)
            return;

        if (string.IsNullOrEmpty(vkVideoLiveSettings.ApiAccessToken))
            return;
        
        // Останавливаем переподключение к старому каналу
        _intentionalDisconnect = true;
        CancelReconnectLoop();

        if (newChannel.Length == 0)
        {
            _channelId = 0;
            UpdateVkVideoLiveDbSettings(vkVideoLiveSettings);
            await _chatClient.DisconnectAsync();
            return;
        }

        var channelInfo = await _vkVideoLiveApiService.GetChannelInfoAsync(
            vkVideoLiveSettings.ApiAccessToken, newChannel);

        _channelName = newChannel;
        _channelId = channelInfo.Data.Channel.Id;
        UpdateVkVideoLiveDbSettings(vkVideoLiveSettings);

        var wsTokenResponse = await _vkVideoLiveApiService.GetWebSocketTokenAsync(vkVideoLiveSettings.ApiAccessToken);
        var wsAccessToken = wsTokenResponse.Data.Token;
        _databaseService.UpdateVkVideoLiveTokens(vkVideoLiveSettings.ApiAccessToken, wsAccessToken);

        await ConnectAsync(new VkVideoLiveConnectionParams(
            OldChannelName: oldChannel,
            ChannelName: newChannel,
            ChannelId: _channelId,
            WsAccessToken: wsAccessToken));
    }

    public Task LoadGlobalDataAsync()
    {
        throw new NotImplementedException();
    }

    public Task LoadChannelDataAsync()
    {
        throw new NotImplementedException();
    }

    private void StartReconnectLoop()
    {
        lock (_reconnectLock)
        {
            if (_reconnectCts != null && !_reconnectCts.IsCancellationRequested)
                return;

            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _reconnectCts = new CancellationTokenSource();
            _ = Task.Run(() => ReconnectLoopAsync(_reconnectCts.Token));
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
    ///     Цикл переподключения с фиксированным интервалом 5с.
    ///     Перед каждой попыткой запрашивает свежий WS-токен через API.
    /// </summary>
    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                var settings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
                if (string.IsNullOrEmpty(settings.ApiAccessToken))
                {
                    Console.WriteLine("[VkVideoLive] Нет API токена — переподключение невозможно");
                    return;
                }

                // Получаем свежий WS-токен (старый мог истечь)
                var wsTokenResponse = await _vkVideoLiveApiService.GetWebSocketTokenAsync(settings.ApiAccessToken);
                var wsToken = wsTokenResponse.Data.Token;
                _databaseService.UpdateVkVideoLiveTokens(settings.ApiAccessToken, wsToken);

                await _chatClient.ConnectAsync(wsToken, _channelId, _channelName);
                Console.WriteLine("[VkVideoLive] Переподключение успешно");
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VkVideoLive] Ошибка переподключения: {ex.Message}");
            }
        }
    }

    private void UpdateVkVideoLiveDbSettings(VkVideoLiveSettings vkVideoLiveSettings)
    {
        vkVideoLiveSettings.Channel = _channelName;
        vkVideoLiveSettings.ChannelId = _channelId;
        _databaseContext.VkVideoLiveSettingsRepository.Save(vkVideoLiveSettings);
    }

    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromVkVideoLiveMessageNotification(message));
    }
}