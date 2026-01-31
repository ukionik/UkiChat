using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using UkiChat.Configuration;
using UkiChat.Entities;
using UkiChat.Model.Chat;
using UkiChat.Repositories;
using UkiChat.Repositories.Memory;

namespace UkiChat.Services;

public class StreamService : IStreamService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly IDatabaseService _databaseService;
    private readonly ILocalizationService _localizationService;
    private readonly ISignalRService _signalRService;
    private readonly ITwitchApiService _twitchApiService;
    private readonly ISevenTvApiService _sevenTvApiService;
    private readonly IChatBadgesRepository _chatBadgesRepository;
    private readonly ISevenTvEmotesRepository _sevenTvEmotesRepository;
    private readonly IVkVideoLiveChatService _vkVideoLiveChatService;
    private readonly IVkVideoLiveApiService _vkVideoLiveApiService;
    private readonly TwitchClient _twitchClient = new();
    private string _channelName = "";
    private string _broadcasterId = "";
    private string _vkVideoLiveChannel = "";

    public StreamService(IDatabaseContext databaseContext
        , ISignalRService signalRService
        , ILocalizationService localizationService
        , ITwitchApiService twitchApiService
        , ISevenTvApiService sevenTvApiService
        , IDatabaseService databaseService
        , IChatBadgesRepository chatBadgesRepository
        , ISevenTvEmotesRepository sevenTvEmotesRepository
        , IVkVideoLiveChatService vkVideoLiveChatService
        , IVkVideoLiveApiService vkVideoLiveApiService)
    {
        _databaseContext = databaseContext;
        _signalRService = signalRService;
        _localizationService = localizationService;
        _twitchApiService = twitchApiService;
        _sevenTvApiService = sevenTvApiService;
        _databaseService = databaseService;
        _chatBadgesRepository = chatBadgesRepository;
        _sevenTvEmotesRepository = sevenTvEmotesRepository;
        _vkVideoLiveChatService = vkVideoLiveChatService;
        _vkVideoLiveApiService = vkVideoLiveApiService;

        // Twitch events
        _twitchClient.OnMessageReceived += async (sender, e) =>
        {
            var badgeUrls = ResolveBadgeUrls(e.ChatMessage);
            var sevenTvEmotes = GetSevenTvEmotes();
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

        // VK Video Live events
        _vkVideoLiveChatService.MessageReceived += async (sender, e) =>
        {
            Console.WriteLine($"[VkVideoLive] Message received: {e.Data}");
            await signalRService.SendChatMessageAsync(UkiChatMessage.FromVkVideoLiveMessage(e.Data));
        };

        _vkVideoLiveChatService.Connected += async (sender, e) =>
        {
            Console.WriteLine("[VkVideoLive] Connected");
            await SendChatMessageNotification(string.Format(_localizationService.GetString("vkvideolive.connectedToChannel"),
                _vkVideoLiveChannel));
        };

        _vkVideoLiveChatService.Disconnected += async (sender, e) =>
        {
            Console.WriteLine($"[VkVideoLive] Disconnected: {e.Reason}");
            await SendChatMessageNotification(
                string.Format(_localizationService.GetString("vkvideolive.disconnectedFromChannel"), _vkVideoLiveChannel));
        };

        _vkVideoLiveChatService.Error += (sender, e) =>
        {
            Console.WriteLine($"[VkVideoLive] Error: {e.Message}");
        };
    }

    public async Task ConnectToTwitchAsync()
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

    public async Task ConnectToVkVideoLiveAsync()
    {
        var vkSettings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();

        if (string.IsNullOrEmpty(vkSettings.Channel))
        {
            Console.WriteLine("[VkVideoLive] Channel not configured");
            return;
        }

        if (string.IsNullOrEmpty(vkSettings.ApiClientId) || string.IsNullOrEmpty(vkSettings.ApiClientSecret))
        {
            Console.WriteLine("[VkVideoLive] API credentials not configured");
            return;
        }

        try
        {
            // Получаем access token если его нет
            var accessToken = vkSettings.ApiAccessToken;
            if (string.IsNullOrEmpty(accessToken))
            {
                var tokenResponse = await _vkVideoLiveApiService.GetAccessTokenAsync(
                    vkSettings.ApiClientId,
                    vkSettings.ApiClientSecret);
                accessToken = tokenResponse.AccessToken;

                // Сохраняем токен в базу
                vkSettings.ApiAccessToken = accessToken;
                _databaseContext.VkVideoLiveSettingsRepository.Save(vkSettings);
            }

            // Получаем информацию о канале для получения chat channel
            var channelInfo = await _vkVideoLiveApiService.GetChannelInfoAsync(accessToken, vkSettings.Channel);
            var chatChannel = channelInfo.Data.Channel.WebSocketChannels?.Chat;

            if (string.IsNullOrEmpty(chatChannel))
            {
                Console.WriteLine("[VkVideoLive] Chat channel not found");
                return;
            }

            _vkVideoLiveChannel = vkSettings.Channel;
            await SendChatMessageNotification(string.Format(_localizationService.GetString("vkvideolive.connectingToChannel"),
                _vkVideoLiveChannel));

            // Подключаемся к чату
            await _vkVideoLiveChatService.ConnectAsync(accessToken, chatChannel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VkVideoLive] Connection error: {ex.Message}");
            await SendChatMessageNotification(string.Format(_localizationService.GetString("vkvideolive.connectingToChannelError"),
                vkSettings.Channel));
        }
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

    private async Task LoadChatBadgesAsync(TwitchSettings twitchSettings)
    {
        try
        {
            // Загружаем глобальные бейджи
            var globalBadges = await _twitchApiService.GetGlobalChatBadgesAsync();
            _chatBadgesRepository.SetGlobalBadges(globalBadges);
            Console.WriteLine($"Loaded {globalBadges.EmoteSet.Length} global badge sets");

            // Загружаем бейджи канала (если есть имя канала)
            if (!string.IsNullOrEmpty(twitchSettings.Channel))
            {
                var broadcasterId = await _twitchApiService.GetBroadcasterIdAsync(twitchSettings.Channel);
                if (!string.IsNullOrEmpty(broadcasterId))
                {
                    _broadcasterId = broadcasterId;
                    var channelBadges = await _twitchApiService.GetChannelChatBadgesAsync(broadcasterId);
                    _chatBadgesRepository.SetChannelBadges(broadcasterId, channelBadges);
                    Console.WriteLine($"Loaded {channelBadges.EmoteSet.Length} channel badge sets for {twitchSettings.Channel}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading chat badges: {ex.Message}");
        }
    }

    private List<string> ResolveBadgeUrls(ChatMessage chatMessage)
    {
        return _chatBadgesRepository.GetBadgeUrls(chatMessage.Badges, _broadcasterId);
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

    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
}