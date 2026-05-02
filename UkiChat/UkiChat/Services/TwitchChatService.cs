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
using UkiChat.Model.Bttv;
using UkiChat.Model.Chat;
using UkiChat.Model.Ffz;
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
    private readonly IFfzApiService _ffzApiService;
    private readonly IFfzEmotesRepository _ffzEmotesRepository;
    private readonly IBttvApiService _bttvApiService;
    private readonly IBttvEmotesRepository _bttvEmotesRepository;
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
        , IFfzApiService ffzApiService
        , IFfzEmotesRepository ffzEmotesRepository
        , IBttvApiService bttvApiService
        , IBttvEmotesRepository bttvEmotesRepository
        , ITwitchApiService twitchApiService
    )
    {
        _databaseContext = databaseContext;
        _signalRService = signalRService;
        _sevenTvApiService = sevenTvApiService;
        _twitchBadgesRepository = twitchBadgesRepository;
        _sevenTvEmotesRepository = sevenTvEmotesRepository;
        _ffzApiService = ffzApiService;
        _ffzEmotesRepository = ffzEmotesRepository;
        _bttvApiService = bttvApiService;
        _bttvEmotesRepository = bttvEmotesRepository;
        _twitchApiService = twitchApiService;

        _twitchClient.OnMessageReceived += async (_, e) =>
        {
            var badgeUrls = ResolveBadgeUrls(e.ChatMessage);
            var thirdPartyEmotes = GetThirdPartyEmotes();
            Console.WriteLine(
                $"[Twitch] Message received from: {e.ChatMessage.DisplayName}. Message: {e.ChatMessage.Message}");
            await signalRService.SendChatMessageAsync(
                UkiChatMessage.FromTwitchMessage(e.ChatMessage, badgeUrls, thirdPartyEmotes));
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
        {
            UpdateTwitchDbSettings(twitchSettings);
            if (oldChannel != null) 
                await _twitchClient.LeaveChannelAsync(oldChannel);
            return;
        }
        
        _channelName = newChannel;
        _broadcasterId = await LoadBroadcasterIdAsync(newChannel);
        UpdateTwitchDbSettings(twitchSettings);
        await Task.WhenAll(
            LoadChannelDataAsync(),
            ConnectAsync(TwitchConnectionParams.OfTwitchSettings(oldChannel ?? "", newChannel, twitchSettings))            
        );
    }

    public async Task LoadGlobalDataAsync()
    {
        await LoadTwitchGlobalBadgesAsync();
        await Task.WhenAll(
            LoadSevenTvGlobalEmotesAsync(),
            LoadFfzGlobalEmotesAsync(),
            LoadBttvGlobalEmotesAsync());
    }

    public async Task LoadChannelDataAsync()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(twitchSettings.Channel))
            return;

        await LoadTwitchChannelBadgesAsync(twitchSettings);
        await Task.WhenAll(
            LoadSevenTvChannelEmotesAsync(twitchSettings),
            LoadFfzChannelEmotesAsync(twitchSettings),
            LoadBttvChannelEmotesAsync(twitchSettings));
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

    private async Task LoadFfzGlobalEmotesAsync()
    {
        // Сначала загружаем из базы — как fallback если API недоступен
        var dbEmotes = _databaseContext.FfzEmoteRepository.GetGlobalEmotes();
        if (dbEmotes.Count > 0)
        {
            _ffzEmotesRepository.SetGlobalEmotes(dbEmotes.Select(e => new FfzEmote(e.EmoteId, e.Name, e.Url)).ToList());
            Console.WriteLine($"Loaded {dbEmotes.Count} global FFZ emotes from DB");
        }

        try
        {
            var globalEmotes = await _ffzApiService.GetGlobalEmotesAsync();
            _ffzEmotesRepository.SetGlobalEmotes(globalEmotes);
            _databaseContext.FfzEmoteRepository.SaveGlobalEmotes(
                globalEmotes.Select(e => new FfzEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            Console.WriteLine($"Loaded {globalEmotes.Count} global FFZ emotes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading global FFZ emotes: {ex.Message}");
        }
    }

    private async Task LoadFfzChannelEmotesAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.ApiBroadcasterId))
            return;

        var broadcasterId = twitchSettings.ApiBroadcasterId;

        // Сначала загружаем из базы — как fallback если API недоступен
        var dbEmotes = _databaseContext.FfzEmoteRepository.GetChannelEmotes(broadcasterId);
        if (dbEmotes.Count > 0)
        {
            _ffzEmotesRepository.SetChannelEmotes(broadcasterId, dbEmotes.Select(e => new FfzEmote(e.EmoteId, e.Name, e.Url)).ToList());
            Console.WriteLine($"Loaded {dbEmotes.Count} channel FFZ emotes for {twitchSettings.Channel} from DB");
        }

        try
        {
            var channelEmotes = await _ffzApiService.GetChannelEmotesAsync(broadcasterId);
            _ffzEmotesRepository.SetChannelEmotes(broadcasterId, channelEmotes);
            _databaseContext.FfzEmoteRepository.SaveChannelEmotes(
                broadcasterId,
                channelEmotes.Select(e => new FfzEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            Console.WriteLine($"Loaded {channelEmotes.Count} channel FFZ emotes for {twitchSettings.Channel}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading channel={twitchSettings.Channel} FFZ emotes: {ex.Message}");
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

    private Dictionary<string, string> GetThirdPartyEmotes()
    {
        var emotes = new Dictionary<string, string>();

        // Приоритет (возрастающий): FFZ → BTTV → 7TV
        foreach (var (name, emote) in _ffzEmotesRepository.GetGlobalEmotes()) emotes[name] = emote.Url;
        foreach (var (name, emote) in _bttvEmotesRepository.GetGlobalEmotes()) emotes[name] = emote.Url;
        foreach (var (name, emote) in _sevenTvEmotesRepository.GetGlobalEmotes()) emotes[name] = emote.Url;

        if (string.IsNullOrEmpty(_broadcasterId))
            return emotes;

        foreach (var (name, emote) in _ffzEmotesRepository.GetChannelEmotes(_broadcasterId)) emotes[name] = emote.Url;
        foreach (var (name, emote) in _bttvEmotesRepository.GetChannelEmotes(_broadcasterId)) emotes[name] = emote.Url;
        foreach (var (name, emote) in _sevenTvEmotesRepository.GetChannelEmotes(_broadcasterId)) emotes[name] = emote.Url;

        return emotes;
    }

    private async Task LoadBttvGlobalEmotesAsync()
    {
        var dbEmotes = _databaseContext.BttvEmoteRepository.GetGlobalEmotes();
        if (dbEmotes.Count > 0)
        {
            _bttvEmotesRepository.SetGlobalEmotes(dbEmotes.Select(e => new BttvEmote(e.EmoteId, e.Name, e.Url)).ToList());
            Console.WriteLine($"Loaded {dbEmotes.Count} global BTTV emotes from DB");
        }

        try
        {
            var globalEmotes = await _bttvApiService.GetGlobalEmotesAsync();
            _bttvEmotesRepository.SetGlobalEmotes(globalEmotes);
            _databaseContext.BttvEmoteRepository.SaveGlobalEmotes(
                globalEmotes.Select(e => new BttvEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            Console.WriteLine($"Loaded {globalEmotes.Count} global BTTV emotes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading global BTTV emotes: {ex.Message}");
        }
    }

    private async Task LoadBttvChannelEmotesAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.ApiBroadcasterId))
            return;

        var broadcasterId = twitchSettings.ApiBroadcasterId;

        var dbEmotes = _databaseContext.BttvEmoteRepository.GetChannelEmotes(broadcasterId);
        if (dbEmotes.Count > 0)
        {
            _bttvEmotesRepository.SetChannelEmotes(broadcasterId, dbEmotes.Select(e => new BttvEmote(e.EmoteId, e.Name, e.Url)).ToList());
            Console.WriteLine($"Loaded {dbEmotes.Count} channel BTTV emotes for {twitchSettings.Channel} from DB");
        }

        try
        {
            var channelEmotes = await _bttvApiService.GetChannelEmotesAsync(broadcasterId);
            _bttvEmotesRepository.SetChannelEmotes(broadcasterId, channelEmotes);
            _databaseContext.BttvEmoteRepository.SaveChannelEmotes(
                broadcasterId,
                channelEmotes.Select(e => new BttvEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            Console.WriteLine($"Loaded {channelEmotes.Count} channel BTTV emotes for {twitchSettings.Channel}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading channel={twitchSettings.Channel} BTTV emotes: {ex.Message}");
        }
    }

    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
}
