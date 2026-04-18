using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using UkiChat.Configuration;
using UkiChat.Entities;
using UkiChat.Model.Chat;
using UkiChat.Model.SevenTv;
using UkiChat.Model.Twitch;
using UkiChat.Repositories.Memory;
using UkiChat.Repositories.Database;

namespace UkiChat.Services;

public class TwitchChatService : ITwitchChatService
{
    // Задержки переподключения: 5с, 10с, 20с, 40с, 80с, 160с, 300с (5 мин макс)
    private static readonly TimeSpan[] ReconnectDelays =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(40),
        TimeSpan.FromSeconds(80),
        TimeSpan.FromSeconds(160),
        TimeSpan.FromSeconds(300),
    ];

    private readonly IDatabaseContext _databaseContext;
    private readonly ILocalizationService _localizationService;
    private readonly ISevenTvApiService _sevenTvApiService;
    private readonly ISevenTvEmotesRepository _sevenTvEmotesRepository;
    private readonly ISignalRService _signalRService;
    private readonly ITwitchApiService _twitchApiService;
    private readonly ITwitchBadgesRepository _twitchBadgesRepository;
    private readonly TwitchClient _twitchClient = new();
    private string _broadcasterId = "";
    private string _channelName = "";

    // true — разрыв инициирован намеренно (смена канала / обновление настроек), переподключаться не нужно
    private bool _intentionalDisconnect;
    private CancellationTokenSource? _reconnectCts;

    public TwitchChatService(IDatabaseContext databaseContext
        , ISignalRService signalRService
        , ILocalizationService localizationService
        , ISevenTvApiService sevenTvApiService
        , ITwitchBadgesRepository twitchBadgesRepository
        , ISevenTvEmotesRepository sevenTvEmotesRepository
        , ITwitchApiService twitchApiService
    )
    {
        _databaseContext = databaseContext;
        _signalRService = signalRService;
        _localizationService = localizationService;
        _sevenTvApiService = sevenTvApiService;
        _twitchBadgesRepository = twitchBadgesRepository;
        _sevenTvEmotesRepository = sevenTvEmotesRepository;
        _twitchApiService = twitchApiService;

        _twitchClient.OnMessageReceived += async (_, e) =>
        {
            var badgeUrls = ResolveBadgeUrls(e.ChatMessage);
            var sevenTvEmotes = GetSevenTvEmotes();
            Console.WriteLine(
                $"[Twitch] Message received from: {e.ChatMessage.DisplayName}. Message: {e.ChatMessage.Message}");
            await signalRService.SendChatMessageAsync(
                UkiChatMessage.FromTwitchMessage(e.ChatMessage, badgeUrls, sevenTvEmotes));
        };

        _twitchClient.OnError += (_, e) =>
        {
            Console.WriteLine(e.Exception.ToString());
            return Task.CompletedTask;
        };

        _twitchClient.OnJoinedChannel += async (_, e) =>
        {
            Console.WriteLine("JoinedChannel");
            await SendChatMessageNotification(string.Format(localizationService.GetString("twitch.connectedToChannel"),
                e.Channel));
        };

        _twitchClient.OnLeftChannel += async (_, e) =>
        {
            Console.WriteLine($"[Twitch] Left channel: {e.Channel}");
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("twitch.disconnectedFromChannel"), e.Channel));
        };

        _twitchClient.OnDisconnected += (_, _) =>
        {
            Console.WriteLine("[Twitch] Disconnected");
            if (!_intentionalDisconnect)
                StartReconnectLoop();
            return Task.CompletedTask;
        };

        _twitchClient.OnConnectionError += (_, e) =>
        {
            Console.WriteLine($"[Twitch] ConnectionError: {e.Error.Message}");
            if (!_intentionalDisconnect)
                StartReconnectLoop();
            return Task.CompletedTask;
        };

        // Watch streak — Twitch шлёт как USERNOTICE viewermilestone, TwitchLib не обрабатывает нативно
        _twitchClient.OnUnaccountedFor += async (_, e) =>
        {
            var watchStreak = TwitchWatchStreak.ParseFromRawIrc(e.RawIRC);
            if (watchStreak == null) return;
            Console.WriteLine($"[Twitch] Watch streak: {watchStreak.DisplayName} x{watchStreak.StreakCount}");
            await signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchWatchStreak(watchStreak));
        };
    }

    public async Task ConnectAsync(TwitchConnectionParams connectionParams)
    {
        // Отменяем текущий цикл переподключения перед новым подключением
        CancelReconnectLoop();
        _intentionalDisconnect = false;

        _broadcasterId = connectionParams.BroadcasterId;

        if (!_twitchClient.IsConnected)
        {
            var credentials =
                new ConnectionCredentials(connectionParams.ChatbotUsername, connectionParams.ChatbotAccessToken);
            _twitchClient.Initialize(credentials);
            await _twitchClient.ConnectAsync();
        }

        if (_twitchClient.JoinedChannels.Any(x => x.Channel == connectionParams.OldChannel))
            await _twitchClient.LeaveChannelAsync(connectionParams.OldChannel);

        if (connectionParams.NewChannel == "")
            return;

        await _twitchClient.JoinChannelAsync(connectionParams.NewChannel, true);
    }

    public async Task ChangeChannelAsync(string newChannel)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var oldChannel = twitchSettings.Channel;
        if (oldChannel == newChannel)
            return;

        if (newChannel.Length == 0)
            return;

        // Останавливаем переподключение к старому каналу
        _intentionalDisconnect = true;
        CancelReconnectLoop();

        _channelName = newChannel;
        _broadcasterId = await LoadBroadcasterIdAsync(newChannel);
        UpdateTwitchDbSettings(twitchSettings);
        await LoadChannelDataAsync();
        await ConnectAsync(TwitchConnectionParams.OfTwitchSettings(oldChannel ?? "", newChannel, twitchSettings));
    }

    public async Task LoadGlobalDataAsync()
    {
        await LoadTwitchGlobalBadgesAsync();
        await LoadSevenTvGlobalEmotesAsync();
    }

    public async Task LoadChannelDataAsync()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(twitchSettings.Channel))
            return;

        await LoadTwitchChannelBadgesAsync(twitchSettings);
        await LoadSevenTvChannelEmotesAsync(twitchSettings);
    }

    private async Task LoadTwitchGlobalBadgesAsync()
    {
        try
        {
            var twitchGlobalBadges = await _twitchApiService.GetGlobalChatBadgesAsync();
            _twitchBadgesRepository.SetGlobalBadges(twitchGlobalBadges);
            Console.WriteLine($"Loaded {twitchGlobalBadges.EmoteSet.Length} global badge sets");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading twitch global badges: {e.Message}");
        }
    }

    private async Task LoadSevenTvGlobalEmotesAsync()
    {
        // Сначала загружаем из базы — как fallback если API недоступен
        var dbEmotes = _databaseContext.SevenTvEmoteRepository.GetGlobalEmotes();
        if (dbEmotes.Count > 0)
        {
            _sevenTvEmotesRepository.SetGlobalEmotes(dbEmotes.Select(e => new SevenTvEmote(e.EmoteId, e.Name, e.Url)).ToList());
            Console.WriteLine($"Loaded {dbEmotes.Count} global 7TV emotes from DB");
        }

        try
        {
            var globalEmotes = await _sevenTvApiService.GetGlobalEmotesAsync();
            _sevenTvEmotesRepository.SetGlobalEmotes(globalEmotes);
            _databaseContext.SevenTvEmoteRepository.SaveGlobalEmotes(
                globalEmotes.Select(e => new SevenTvEmoteEntity {EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            Console.WriteLine($"Loaded {globalEmotes.Count} global 7TV emotes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Global 7TV emotes: {ex.Message}");
        }
    }

    private async Task LoadTwitchChannelBadgesAsync(TwitchSettings twitchSettings)
    {
        try
        {
            if (string.IsNullOrEmpty(twitchSettings.ApiBroadcasterId))
                return;

            var channelBadges = await _twitchApiService.GetChannelChatBadgesAsync(twitchSettings.ApiBroadcasterId);
            _twitchBadgesRepository.SetChannelBadges(twitchSettings.ApiBroadcasterId, channelBadges);
            Console.WriteLine($"Loaded {channelBadges.EmoteSet.Length} channel badge sets for {twitchSettings.Channel}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading twitch chat badges: {e.Message}");
        }
    }

    private async Task LoadSevenTvChannelEmotesAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.ApiBroadcasterId))
            return;

        var broadcasterId = twitchSettings.ApiBroadcasterId;

        // Сначала загружаем из базы — как fallback если API недоступен
        var dbEmotes = _databaseContext.SevenTvEmoteRepository.GetChannelEmotes(broadcasterId);
        if (dbEmotes.Count > 0)
        {
            _sevenTvEmotesRepository.SetChannelEmotes(broadcasterId, dbEmotes.Select(e => new SevenTvEmote(e.EmoteId, e.Name, e.Url)).ToList());
            Console.WriteLine($"Loaded {dbEmotes.Count} channel 7TV emotes for {twitchSettings.Channel} from DB");
        }

        try
        {
            var channelEmotes = await _sevenTvApiService.GetChannelEmotesAsync(broadcasterId);
            _sevenTvEmotesRepository.SetChannelEmotes(broadcasterId, channelEmotes);
            _databaseContext.SevenTvEmoteRepository.SaveChannelEmotes(
                broadcasterId,
                channelEmotes.Select(e => new SevenTvEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            Console.WriteLine($"Loaded {channelEmotes.Count} channel 7TV emotes for {twitchSettings.Channel}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading channel={twitchSettings.Channel} 7TV emotes: {ex.Message}");
        }
    }

    private void StartReconnectLoop()
    {
        CancelReconnectLoop();
        _reconnectCts = new CancellationTokenSource();
        _ = Task.Run(() => ReconnectLoopAsync(_reconnectCts.Token));
    }

    private void CancelReconnectLoop()
    {
        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _reconnectCts = null;
    }

    /// <summary>
    ///     Бесконечный цикл переподключения с экспоненциальным увеличением интервала.
    /// </summary>
    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; !cancellationToken.IsCancellationRequested; attempt++)
        {
            var delay = ReconnectDelays[Math.Min(attempt, ReconnectDelays.Length - 1)];
            Console.WriteLine($"[Twitch] Переподключение через {delay.TotalSeconds}с (попытка {attempt + 1})");

            await SendChatMessageNotification(string.Format(
                _localizationService.GetString("twitch.reconnectingInSeconds"),
                (int)delay.TotalSeconds,
                attempt + 1));

            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                var settings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
                if (string.IsNullOrEmpty(settings.Channel))
                {
                    Console.WriteLine("[Twitch] Канал не задан — переподключение невозможно");
                    return;
                }

                // ReconnectAsync переподключает IRC и автоматически заходит в ранее joined каналы
                await _twitchClient.ReconnectAsync();
                Console.WriteLine("[Twitch] Переподключение успешно");
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Twitch] Ошибка переподключения (попытка {attempt + 1}): {ex.Message}");
            }
        }
    }

    private void UpdateTwitchDbSettings(TwitchSettings twitchSettings)
    {
        twitchSettings.Channel = _channelName;
        twitchSettings.ApiBroadcasterId = _broadcasterId;
        _databaseContext.TwitchSettingsRepository.Save(twitchSettings);
    }

    private async Task<string> LoadBroadcasterIdAsync(string newChannel)
    {
        try
        {
            return await _twitchApiService.GetBroadcasterIdAsync(newChannel);
        }
        catch (Exception)
        {
            Console.WriteLine($"Can't load Twitch API Broadcaster Id for Channel={newChannel}");
            return "";
        }
    }

    private List<string> ResolveBadgeUrls(ChatMessage chatMessage)
    {
        return _twitchBadgesRepository.GetBadgeUrls(chatMessage.Badges, _broadcasterId);
    }

    private Dictionary<string, SevenTvEmote> GetSevenTvEmotes()
    {
        var emotes = new Dictionary<string, SevenTvEmote>();

        foreach (var (name, emote) in _sevenTvEmotesRepository.GetGlobalEmotes()) emotes[name] = emote;

        if (string.IsNullOrEmpty(_broadcasterId))
            return emotes;

        foreach (var (name, emote) in _sevenTvEmotesRepository.GetChannelEmotes(_broadcasterId)) emotes[name] = emote;

        return emotes;
    }

    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
}