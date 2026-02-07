using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using UkiChat.Configuration;
using UkiChat.Entities;
using UkiChat.Model.Chat;
using UkiChat.Repositories.Memory;

namespace UkiChat.Services;

public class TwitchChatService : ITwitchChatService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly IDatabaseService _databaseService;
    private readonly ISignalRService _signalRService;
    private readonly ILocalizationService _localizationService;
    private readonly TwitchClient _twitchClient = new();
    private readonly ITwitchBadgesRepository _twitchBadgesRepository;
    private readonly ITwitchApiService _twitchApiService;
    private readonly ISevenTvApiService _sevenTvApiService;
    private readonly ISevenTvEmotesRepository _sevenTvEmotesRepository;
    private string _broadcasterId = "";
    private string _channelName = "";
    
    public TwitchChatService(IDatabaseContext databaseContext
        , IDatabaseService databaseService
        , ISignalRService signalRService   
        , ILocalizationService localizationService
        , ITwitchApiService twitchApiService
        , ISevenTvApiService sevenTvApiService, ITwitchBadgesRepository twitchBadgesRepository, ISevenTvEmotesRepository sevenTvEmotesRepository)
    {
        _databaseContext = databaseContext;
        _databaseService = databaseService;
        _signalRService = signalRService;
        _localizationService = localizationService;
        _twitchApiService = twitchApiService;
        _sevenTvApiService = sevenTvApiService;
        _twitchBadgesRepository = twitchBadgesRepository;
        _sevenTvEmotesRepository = sevenTvEmotesRepository;

        _twitchClient.OnMessageReceived += async (sender, e) =>
        {
            var badgeUrls = ResolveBadgeUrls(e.ChatMessage);
            var sevenTvEmotes = GetSevenTvEmotes();
            Console.WriteLine($"[Twitch] Message received from: {e.ChatMessage.DisplayName}. Message: {e.ChatMessage.Message}");
            await signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessage(e.ChatMessage, badgeUrls, sevenTvEmotes));
        };

        _twitchClient.OnError += async (sender, e) =>
        {
            Console.WriteLine(e.Exception.ToString());
            /*await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.error"), _channelName));*/
        };

        _twitchClient.OnJoinedChannel += async (sender, e) =>
        {
            Console.WriteLine("JoinedChannel");
            await SendChatMessageNotification(string.Format(_localizationService.GetString("twitch.connectedToChannel"),
                e.Channel));
        };

        _twitchClient.OnLeftChannel += async (sender, e) =>
        {
            Console.WriteLine("Disconnected");
            await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.disconnectedFromChannel"), e.Channel));
        };

        _twitchClient.OnDisconnected += async (sender, e) =>
        {
            Console.WriteLine("Disconnected");
            /*await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.disconnectedFromChannel")));*/
        };

        _twitchClient.OnConnectionError += async (sender, e) =>
        {
            Console.WriteLine("ConnectionError");
            /*await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.connectingToChannelError"), _channelName));*/
        };
    }

    public async Task ConnectAsync()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var oldChannel = _channelName;
        var newChannel = twitchSettings.Channel;

        // Инициализация Twitch API (если есть credentials)
        await InitializeTwitchApiAsync(twitchSettings);

        // Загрузка бейджей чата
        await LoadChatBadgesAsync(twitchSettings);

        // Загрузка 7TV эмоутов
        await LoadSevenTvEmotesAsync(twitchSettings);

        if (!_twitchClient.IsConnected)
        {
            var credentials =
                new ConnectionCredentials(twitchSettings.ChatbotUsername, twitchSettings.ChatbotAccessToken);
            _twitchClient.Initialize(credentials);
            await _twitchClient.ConnectAsync();
        }

        if (oldChannel == newChannel) return;

        if (_twitchClient.JoinedChannels.Any(x => x.Channel == oldChannel))
            await _twitchClient.LeaveChannelAsync(oldChannel);

        if (newChannel.Length == 0) return;

        _channelName = newChannel;
        await SendChatMessageNotification(string.Format(_localizationService.GetString("twitch.connectingToChannel"),
            _channelName));

        await _twitchClient.JoinChannelAsync(newChannel, true);
    }
    
    private async Task InitializeTwitchApiAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.ApiClientId) ||
            string.IsNullOrEmpty(twitchSettings.ApiClientSecret))
            return;

        await _twitchApiService.InitializeAsync(
            twitchSettings.ApiClientId,
            twitchSettings.ApiAccessToken ?? "");

        // Проверяем валидность токена и обновляем при необходимости
        await RefreshTwitchApiTokensAsync(twitchSettings);
    }


    
    private List<string> ResolveBadgeUrls(ChatMessage chatMessage)
    {
        return _twitchBadgesRepository.GetBadgeUrls(chatMessage.Badges, _broadcasterId);
    }
    
    private Dictionary<string, Model.SevenTv.SevenTvEmote> GetSevenTvEmotes()
    {
        var emotes = new Dictionary<string, Model.SevenTv.SevenTvEmote>();

        // Добавляем глобальные эмоуты
        foreach (var (name, emote) in _sevenTvEmotesRepository.GetGlobalEmotes())
        {
            emotes[name] = emote;
        }

        // Добавляем эмоуты канала (они перезаписывают глобальные с таким же именем)
        if (!string.IsNullOrEmpty(_broadcasterId))
        {
            foreach (var (name, emote) in _sevenTvEmotesRepository.GetChannelEmotes(_broadcasterId))
            {
                emotes[name] = emote;
            }
        }

        return emotes;
    }
    
    private async Task LoadSevenTvEmotesAsync(TwitchSettings twitchSettings)
    {
        try
        {
            // Загружаем глобальные эмоуты 7TV
            var globalEmotes = await _sevenTvApiService.GetGlobalEmotesAsync();
            _sevenTvEmotesRepository.SetGlobalEmotes(globalEmotes);
            Console.WriteLine($"Loaded {globalEmotes.Count} global 7TV emotes");

            // Загружаем эмоуты канала (если есть broadcaster ID)
            if (!string.IsNullOrEmpty(_broadcasterId))
            {
                var channelEmotes = await _sevenTvApiService.GetChannelEmotesAsync(_broadcasterId);
                _sevenTvEmotesRepository.SetChannelEmotes(_broadcasterId, channelEmotes);
                Console.WriteLine($"Loaded {channelEmotes.Count} channel 7TV emotes for {twitchSettings.Channel}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading 7TV emotes: {ex.Message}");
        }
    }
    
    private async Task LoadChatBadgesAsync(TwitchSettings twitchSettings)
    {
        try
        {
            // Загружаем глобальные бейджи
            var globalBadges = await _twitchApiService.GetGlobalChatBadgesAsync();
            _twitchBadgesRepository.SetGlobalBadges(globalBadges);
            Console.WriteLine($"Loaded {globalBadges.EmoteSet.Length} global badge sets");

            // Загружаем бейджи канала (если есть имя канала)
            if (!string.IsNullOrEmpty(twitchSettings.Channel))
            {
                var broadcasterId = await _twitchApiService.GetBroadcasterIdAsync(twitchSettings.Channel);
                if (!string.IsNullOrEmpty(broadcasterId))
                {
                    _broadcasterId = broadcasterId;
                    var channelBadges = await _twitchApiService.GetChannelChatBadgesAsync(broadcasterId);
                    _twitchBadgesRepository.SetChannelBadges(broadcasterId, channelBadges);
                    Console.WriteLine($"Loaded {channelBadges.EmoteSet.Length} channel badge sets for {twitchSettings.Channel}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading chat badges: {ex.Message}");
        }
    }
    
    private async Task RefreshTwitchApiTokensAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.ApiRefreshToken) ||
            string.IsNullOrEmpty(twitchSettings.ApiClientId) ||
            string.IsNullOrEmpty(twitchSettings.ApiClientSecret))
            return;

        var newTokens = await _twitchApiService.EnsureValidTokenAsync(
            twitchSettings.ApiRefreshToken,
            twitchSettings.ApiClientId,
            twitchSettings.ApiClientSecret);

        if (newTokens != null)
        {
            _databaseService.UpdateTwitchApiTokens(newTokens.AccessToken, newTokens.RefreshToken);
            Console.WriteLine("Twitch API tokens refreshed");
        }
    }
    
    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
}