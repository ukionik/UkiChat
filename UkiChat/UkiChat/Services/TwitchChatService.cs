using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using UkiChat.Configuration;
using UkiChat.Diagnostics;
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
    private readonly ILocalizationService _localizationService;
    private readonly ITwitchApiService _twitchApiService;
    private readonly ITwitchBadgesRepository _twitchBadgesRepository;
    private readonly ITwitchChannelPointsRewardsRepository _channelPointsRewardsRepository;
    private readonly ILogger<TwitchChatService> _logger;

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
        , ITwitchChannelPointsRewardsRepository channelPointsRewardsRepository
        , ISevenTvEmotesRepository sevenTvEmotesRepository
        , IFfzApiService ffzApiService
        , IFfzEmotesRepository ffzEmotesRepository
        , IBttvApiService bttvApiService
        , IBttvEmotesRepository bttvEmotesRepository
        , ITwitchApiService twitchApiService
        , ILogger<TwitchChatService> logger
    )
    {
        _databaseContext = databaseContext;
        _signalRService = signalRService;
        _localizationService = localizationService;
        _sevenTvApiService = sevenTvApiService;
        _twitchBadgesRepository = twitchBadgesRepository;
        _channelPointsRewardsRepository = channelPointsRewardsRepository;
        _sevenTvEmotesRepository = sevenTvEmotesRepository;
        _ffzApiService = ffzApiService;
        _ffzEmotesRepository = ffzEmotesRepository;
        _bttvApiService = bttvApiService;
        _bttvEmotesRepository = bttvEmotesRepository;
        _twitchApiService = twitchApiService;
        _logger = logger;

        _twitchClient.OnMessageReceived += async (_, e) =>
        {
            var badgeUrls = ResolveBadgeUrls(e.ChatMessage);
            var thirdPartyEmotes = GetThirdPartyEmotes();
            var rewardTitle = ResolveRewardTitle(e.ChatMessage);
            _logger.LogDebug("Получено сообщение от {DisplayName}: {Message}",
                e.ChatMessage.DisplayName, e.ChatMessage.Message);
            await signalRService.SendChatMessageAsync(
                UkiChatMessage.FromTwitchMessage(e.ChatMessage, badgeUrls, thirdPartyEmotes, rewardTitle));
        };

        _twitchClient.OnError += (_, e) =>
        {
            _logger.LogError(e.Exception, "TwitchClient ошибка");
            return Task.CompletedTask;
        };

        _twitchClient.OnJoinedChannel += async (_, e) =>
        {
            StartupDiagnostics.Log("twitch-chat", $"OnJoinedChannel: {e.Channel}");
            _logger.LogInformation("Присоединились к каналу: {Channel}", e.Channel);
            await SendChatMessageNotification(string.Format(localizationService.GetString("twitch.connectedToChannel"),
                e.Channel));
        };

        _twitchClient.OnLeftChannel += async (_, e) =>
        {
            StartupDiagnostics.Log("twitch-chat", $"OnLeftChannel: {e.Channel}");
            _logger.LogInformation("Покинули канал: {Channel}", e.Channel);
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("twitch.disconnectedFromChannel"), e.Channel));
        };

        _twitchClient.OnConnected += (_, _) =>
        {
            StartupDiagnostics.Log("twitch-chat", "OnConnected (IRC handshake done)");
            _logger.LogInformation("Подключено (IRC handshake done)");
            return Task.CompletedTask;
        };

        // TwitchLib.Communication автоматически переподключается согласно ReconnectionPolicy.
        // OnDisconnected — только логирование; библиотека сама инициирует попытки.
        _twitchClient.OnDisconnected += async (_, _) =>
        {
            StartupDiagnostics.Log("twitch-chat", "OnDisconnected");
            _logger.LogWarning("Отключено от канала: {Channel}", _channelName);
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("twitch.disconnectedFromChannel"), _channelName));
        };

        _twitchClient.OnConnectionError += async (_, e) =>
        {
            StartupDiagnostics.LogError("twitch-chat", $"OnConnectionError: {e.Error?.Message}");
            _logger.LogError("Ошибка подключения: {Error}", e.Error?.Message);
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("twitch.disconnectedFromChannel"), _channelName));
        };

        // Срабатывает когда TwitchLib успешно восстановил соединение.
        // Каналы TwitchLib переходит автоматически → OnJoinedChannel уведомит пользователя.
        _twitchClient.OnReconnected += (_, _) =>
        {
            StartupDiagnostics.Log("twitch-chat", "OnReconnected (TwitchLib auto-reconnect)");
            _logger.LogInformation("Переподключено (TwitchLib auto-reconnect)");
            return Task.CompletedTask;
        };

        _twitchClient.OnMessageCleared += async (_, e) =>
        {
            _logger.LogDebug("Сообщение удалено: {MessageId}", e.TargetMessageId);
            await signalRService.SendMessageDeletedAsync(e.TargetMessageId);
        };

        _twitchClient.OnUserBanned += async (_, e) =>
        {
            _logger.LogInformation("Пользователь забанен: {Username}", e.UserBan.Username);
            await signalRService.SendUserMessagesDeletedAsync(e.UserBan.Username);
        };

        _twitchClient.OnUserTimedout += async (_, e) =>
        {
            _logger.LogInformation("Таймаут пользователя: {Username} ({Duration}с)",
                e.UserTimeout.Username, e.UserTimeout.TimeoutDuration);
            await signalRService.SendUserMessagesDeletedAsync(e.UserTimeout.Username);
        };

        // Watch streak — Twitch шлёт как USERNOTICE viewermilestone, TwitchLib не обрабатывает нативно
        _twitchClient.OnUnaccountedFor += async (_, e) =>
        {
            var watchStreak = TwitchWatchStreak.ParseFromRawIrc(e.RawIRC);
            if (watchStreak == null) return;
            _logger.LogInformation("Watch streak: {DisplayName} x{StreakCount}",
                watchStreak.DisplayName, watchStreak.StreakCount);
            await signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchWatchStreak(watchStreak));
        };

        _twitchClient.OnSendReceiveData += (_, e) =>
        {
            _logger.LogDebug("IRC [{Direction}]: {Data}", e.Direction, e.Data);
            return Task.CompletedTask;
        };
    }

    public async Task ConnectAsync(TwitchConnectionParams connectionParams)
    {
        using var _ = StartupDiagnostics.Measure("twitch-chat",
            $"ConnectAsync(old={connectionParams.OldChannel}, new={connectionParams.NewChannel})");
        _broadcasterId = connectionParams.BroadcasterId;
        _logger.LogInformation("ConnectAsync: старый={OldChannel} новый={NewChannel} broadcasterId={BroadcasterId}",
            connectionParams.OldChannel, connectionParams.NewChannel, connectionParams.BroadcasterId);

        if (!_twitchClient.IsConnected)
        {
            _logger.LogInformation("TwitchClient не подключён — инициализируем и подключаемся (WebSocket)");
            var credentials =
                new ConnectionCredentials(connectionParams.ChatbotUsername, connectionParams.ChatbotAccessToken);
            _twitchClient.Initialize(credentials);
            using (StartupDiagnostics.Measure("twitch-chat", "  TwitchClient.ConnectAsync (WebSocket)"))
            {
                await _twitchClient.ConnectAsync();
            }
        }

        if (_twitchClient.JoinedChannels.Any(x => x.Channel == connectionParams.OldChannel))
        {
            _logger.LogInformation("Покидаем старый канал: {OldChannel}", connectionParams.OldChannel);
            using (StartupDiagnostics.Measure("twitch-chat", $"  LeaveChannelAsync({connectionParams.OldChannel})"))
            {
                await _twitchClient.LeaveChannelAsync(connectionParams.OldChannel);
            }
        }

        if (connectionParams.NewChannel == "")
        {
            _logger.LogInformation("Новый канал пустой — завершаем ConnectAsync");
            return;
        }

        await SendChatMessageNotification(string.Format(_localizationService.GetString("twitch.connectingToChannel"), connectionParams.NewChannel));
        _logger.LogInformation("Входим в канал: {NewChannel}", connectionParams.NewChannel);
        using (StartupDiagnostics.Measure("twitch-chat", $"  JoinChannelAsync({connectionParams.NewChannel})"))
        {
            await _twitchClient.JoinChannelAsync(connectionParams.NewChannel, true);
        }
    }

    public async Task ChangeChannelAsync(string newChannel)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var oldChannel = twitchSettings.Channel;
        _logger.LogInformation("ChangeChannelAsync: старый={OldChannel} новый={NewChannel}", oldChannel, newChannel);

        if (oldChannel == newChannel)
        {
            _logger.LogDebug("ChangeChannelAsync: каналы совпадают, пропускаем");
            return;
        }

        if (newChannel.Length == 0)
        {
            _logger.LogInformation("ChangeChannelAsync: новый канал пустой, отключаемся от {OldChannel}", oldChannel);
            UpdateTwitchDbSettings(twitchSettings);
            if (oldChannel != null)
                await _twitchClient.LeaveChannelAsync(oldChannel);
            return;
        }

        _channelName = newChannel;
        _broadcasterId = await LoadBroadcasterIdAsync(newChannel);
        _logger.LogInformation("ChangeChannelAsync: broadcasterId={BroadcasterId}", _broadcasterId);
        UpdateTwitchDbSettings(twitchSettings);
        await Task.WhenAll(
            LoadChannelDataAsync(),
            ConnectAsync(TwitchConnectionParams.OfTwitchSettings(oldChannel ?? "", newChannel, twitchSettings))
        );
    }

    public async Task LoadGlobalDataAsync()
    {
        using var _ = StartupDiagnostics.Measure("twitch-chat", "LoadGlobalDataAsync");
        await LoadTwitchGlobalBadgesAsync();
        try
        {
            await Task.WhenAll(
                LoadSevenTvGlobalEmotesAsync(),
                LoadFfzGlobalEmotesAsync(),
                LoadBttvGlobalEmotesAsync()).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Загрузка глобальных эмотов 7TV/FFZ/BTTV прервана по таймауту (5с)");
        }
    }

    public async Task LoadChannelDataAsync()
    {
        using var _ = StartupDiagnostics.Measure("twitch-chat", "LoadChannelDataAsync");
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(twitchSettings.Channel))
        {
            StartupDiagnostics.Log("twitch-chat", "  no channel configured, skipping channel data");
            return;
        }

        await LoadTwitchChannelBadgesAsync(twitchSettings);
        try
        {
            await Task.WhenAll(
                LoadSevenTvChannelEmotesAsync(twitchSettings),
                LoadFfzChannelEmotesAsync(twitchSettings),
                LoadBttvChannelEmotesAsync(twitchSettings)).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Загрузка эмотов канала 7TV/FFZ/BTTV прервана по таймауту (5с)");
        }

        await LoadCustomRewardsAsync(twitchSettings);
    }

    private async Task LoadTwitchGlobalBadgesAsync()
    {
        try
        {
            var twitchGlobalBadges = await _twitchApiService.GetGlobalChatBadgesAsync();
            _twitchBadgesRepository.SetGlobalBadges(twitchGlobalBadges);
            _logger.LogInformation("Загружено {Count} глобальных наборов значков Twitch", twitchGlobalBadges.EmoteSet.Length);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка загрузки глобальных значков Twitch");
        }
    }

    private async Task LoadSevenTvGlobalEmotesAsync()
    {
        // Сначала загружаем из базы — как fallback если API недоступен
        var dbEmotes = _databaseContext.SevenTvEmoteRepository.GetGlobalEmotes();
        if (dbEmotes.Count > 0)
        {
            _sevenTvEmotesRepository.SetGlobalEmotes(dbEmotes.Select(e => new SevenTvEmote(e.EmoteId, e.Name, e.Url)).ToList());
            _logger.LogInformation("Загружено {Count} глобальных эмотов 7TV из БД", dbEmotes.Count);
        }

        try
        {
            var globalEmotes = await _sevenTvApiService.GetGlobalEmotesAsync();
            _sevenTvEmotesRepository.SetGlobalEmotes(globalEmotes);
            _databaseContext.SevenTvEmoteRepository.SaveGlobalEmotes(
                globalEmotes.Select(e => new SevenTvEmoteEntity {EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            _logger.LogInformation("Загружено {Count} глобальных эмотов 7TV", globalEmotes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки глобальных эмотов 7TV");
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
            _logger.LogInformation("Загружено {Count} наборов значков канала {Channel}",
                channelBadges.EmoteSet.Length, twitchSettings.Channel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка загрузки значков канала {Channel}", twitchSettings.Channel);
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
            _logger.LogInformation("Загружено {Count} эмотов 7TV канала {Channel} из БД", dbEmotes.Count, twitchSettings.Channel);
        }

        try
        {
            var channelEmotes = await _sevenTvApiService.GetChannelEmotesAsync(broadcasterId);
            _sevenTvEmotesRepository.SetChannelEmotes(broadcasterId, channelEmotes);
            _databaseContext.SevenTvEmoteRepository.SaveChannelEmotes(
                broadcasterId,
                channelEmotes.Select(e => new SevenTvEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            _logger.LogInformation("Загружено {Count} эмотов 7TV канала {Channel}", channelEmotes.Count, twitchSettings.Channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки эмотов 7TV канала {Channel}", twitchSettings.Channel);
        }
    }

    private async Task LoadFfzGlobalEmotesAsync()
    {
        // Сначала загружаем из базы — как fallback если API недоступен
        var dbEmotes = _databaseContext.FfzEmoteRepository.GetGlobalEmotes();
        if (dbEmotes.Count > 0)
        {
            _ffzEmotesRepository.SetGlobalEmotes(dbEmotes.Select(e => new FfzEmote(e.EmoteId, e.Name, e.Url)).ToList());
            _logger.LogInformation("Загружено {Count} глобальных эмотов FFZ из БД", dbEmotes.Count);
        }

        try
        {
            var globalEmotes = await _ffzApiService.GetGlobalEmotesAsync();
            _ffzEmotesRepository.SetGlobalEmotes(globalEmotes);
            _databaseContext.FfzEmoteRepository.SaveGlobalEmotes(
                globalEmotes.Select(e => new FfzEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            _logger.LogInformation("Загружено {Count} глобальных эмотов FFZ", globalEmotes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки глобальных эмотов FFZ");
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
            _logger.LogInformation("Загружено {Count} эмотов FFZ канала {Channel} из БД", dbEmotes.Count, twitchSettings.Channel);
        }

        try
        {
            var channelEmotes = await _ffzApiService.GetChannelEmotesAsync(broadcasterId);
            _ffzEmotesRepository.SetChannelEmotes(broadcasterId, channelEmotes);
            _databaseContext.FfzEmoteRepository.SaveChannelEmotes(
                broadcasterId,
                channelEmotes.Select(e => new FfzEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            _logger.LogInformation("Загружено {Count} эмотов FFZ канала {Channel}", channelEmotes.Count, twitchSettings.Channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки эмотов FFZ канала {Channel}", twitchSettings.Channel);
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
            var id = await _twitchApiService.GetBroadcasterIdAsync(newChannel);
            _logger.LogInformation("Загружен broadcasterId={BroadcasterId} для канала {Channel}", id, newChannel);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось загрузить broadcasterId для канала {Channel}", newChannel);
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
            _logger.LogInformation("Загружено {Count} глобальных эмотов BTTV из БД", dbEmotes.Count);
        }

        try
        {
            var globalEmotes = await _bttvApiService.GetGlobalEmotesAsync();
            _bttvEmotesRepository.SetGlobalEmotes(globalEmotes);
            _databaseContext.BttvEmoteRepository.SaveGlobalEmotes(
                globalEmotes.Select(e => new BttvEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            _logger.LogInformation("Загружено {Count} глобальных эмотов BTTV", globalEmotes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки глобальных эмотов BTTV");
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
            _logger.LogInformation("Загружено {Count} эмотов BTTV канала {Channel} из БД", dbEmotes.Count, twitchSettings.Channel);
        }

        try
        {
            var channelEmotes = await _bttvApiService.GetChannelEmotesAsync(broadcasterId);
            _bttvEmotesRepository.SetChannelEmotes(broadcasterId, channelEmotes);
            _databaseContext.BttvEmoteRepository.SaveChannelEmotes(
                broadcasterId,
                channelEmotes.Select(e => new BttvEmoteEntity { EmoteId = e.Id, Name = e.Name, Url = e.Url }));
            _logger.LogInformation("Загружено {Count} эмотов BTTV канала {Channel}", channelEmotes.Count, twitchSettings.Channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки эмотов BTTV канала {Channel}", twitchSettings.Channel);
        }
    }

    public async Task ReloadCustomRewardsAsync()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        await LoadCustomRewardsAsync(twitchSettings);
    }

    private async Task LoadCustomRewardsAsync(TwitchSettings twitchSettings)
    {
        // Награды можно прочитать только для собственного канала авторизованного пользователя:
        // Twitch требует, чтобы broadcaster_id совпадал с user_id токена.
        if (string.IsNullOrEmpty(twitchSettings.UserId) ||
            string.IsNullOrEmpty(twitchSettings.UserAccessToken))
            return;

        try
        {
            var rewards = await _twitchApiService.GetCustomRewardsAsync(
                twitchSettings.UserId, twitchSettings.UserAccessToken);
            _channelPointsRewardsRepository.SetRewards(twitchSettings.UserId, rewards);
            _logger.LogInformation("Загружено {Count} кастомных наград пользователя {Login}",
                rewards.Count, twitchSettings.UserLogin);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось загрузить кастомные награды пользователя {Login}",
                twitchSettings.UserLogin);
        }
    }

    private string? ResolveRewardTitle(ChatMessage chatMessage)
    {
        if (chatMessage.IsHighlighted)
            return "Highlight My Message";

        // Названия наград известны только для собственного канала пользователя —
        // _broadcasterId (текущий просматриваемый канал) совпадёт с UserId лишь на своём канале.
        if (!string.IsNullOrEmpty(chatMessage.CustomRewardId))
            return _channelPointsRewardsRepository.GetRewardTitle(_broadcasterId, chatMessage.CustomRewardId);

        return null;
    }

    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
}
