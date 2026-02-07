using System;
using System.Threading.Tasks;
using TwitchLib.Client;
using UkiChat.Configuration;
using UkiChat.Model.Chat;

namespace UkiChat.Services;

public class StreamService : IStreamService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly ILocalizationService _localizationService;
    private readonly ISignalRService _signalRService;
    private readonly IVkVideoLiveChatService _vkVideoLiveChatService;
    private readonly IVkVideoLiveApiService _vkVideoLiveApiService;
    private readonly TwitchClient _twitchClient = new();
    private string _vkVideoLiveChannel = "";

    public StreamService(IDatabaseContext databaseContext
        , ILocalizationService localizationService
        , ISignalRService signalRService
        , IVkVideoLiveChatService vkVideoLiveChatService
        , IVkVideoLiveApiService vkVideoLiveApiService)
    {
        _databaseContext = databaseContext;
        _localizationService = localizationService;
        _vkVideoLiveChatService = vkVideoLiveChatService;
        _vkVideoLiveApiService = vkVideoLiveApiService;

        // VK Video Live events
        _vkVideoLiveChatService.MessageReceived += async (sender, e) =>
        {
            if (e.Message == null) return;
            Console.WriteLine($"[VkVideoLive] Message received from: {e.Message.Data?.Author?.DisplayName}");
            await signalRService.SendChatMessageAsync(UkiChatMessage.FromVkVideoLiveMessage(e.Message));
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
    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
}