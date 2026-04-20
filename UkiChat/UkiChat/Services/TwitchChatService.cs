using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using UkiChat.Configuration;
using UkiChat.Entities;
using UkiChat.Model.Chat;
using UkiChat.Model.SevenTv;
using UkiChat.Model.Twitch;
using UkiChat.Repositories.Database;
using UkiChat.Repositories.Memory;

namespace UkiChat.Services;

public class TwitchChatService : ITwitchChatService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly ISevenTvApiService _sevenTvApiService;
    private readonly ISevenTvEmotesRepository _sevenTvEmotesRepository;
    private readonly ISignalRService _signalRService;
    private readonly ITwitchApiService _twitchApiService;
    private readonly ITwitchBadgesRepository _twitchBadgesRepository;

    // ReconnectionPolicy(5000): бесконечные попытки, фиксированный интервал 5с.
    // TwitchLib.Communication сам управляет переподключением — кастомный цикл не нужен.
    private readonly TwitchClient _twitchClient = new(
        new WebSocketClient(new ClientOptions(new ReconnectionPolicy(5_000))));

    private string _broadcasterId = "";
    private string _channelName = "";

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
            Console.WriteLine($"[Twitch] Joined channel: {e.Channel}");
            await SendChatMessageNotification(string.Format(localizationService.GetString("twitch.connectedToChannel"),
                e.Channel));
        };

        _twitchClient.OnLeftChannel += async (_, e) =>
        {
            Console.WriteLine($"[Twitch] Left channel: {e.Channel}");
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("twitch.disconnectedFromChannel"), e.Channel));
        };

        // TwitchLib.Communication автоматически переподключается согласно ReconnectionPolicy.
        // OnDisconnected — только логирование; библиотека сама инициирует попытки.
        _twitchClient.OnDisconnected += async (_, _) =>
        {
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("twitch.disconnectedFromChannel"), _channelName));
        };

        _twitchClient.OnConnectionError += async (_, _) =>
        {
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("twitch.disconnectedFromChannel"), _channelName));
        };

        // Срабатывает когда TwitchLib успешно восстановил соединение.
        // Каналы TwitchLib переходит автоматически → OnJoinedChannel уведомит пользователя.
        _twitchClient.OnReconnected += (_, _) =>
        {
            Console.WriteLine("[Twitch] Переподключён (TwitchLib auto-reconnect)");
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
