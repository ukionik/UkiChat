using System;
using System.Threading.Tasks;
using UkiChat.Configuration;
using UkiChat.Entities;
using UkiChat.Model.Chat;
using UkiChat.Model.VkVideoLive;

namespace UkiChat.Services;

public class VkVideoLiveChatService : IVkVideoLiveChatService
{
    private readonly VkVideoLiveChatClient _chatClient;
    private readonly IDatabaseContext _databaseContext;
    private readonly ILocalizationService _localizationService;
    private readonly ISignalRService _signalRService;
    private readonly IVkVideoLiveApiService _vkVideoLiveApiService;
    private string _channelName = "";

    public VkVideoLiveChatService(IDatabaseContext databaseContext
        , IVkVideoLiveApiService vkVideoLiveApiService
        , ISignalRService signalRService
        , ILocalizationService localizationService)
    {
        _databaseContext = databaseContext;
        _vkVideoLiveApiService = vkVideoLiveApiService;
        _signalRService = signalRService;
        _localizationService = localizationService;
        _chatClient = new VkVideoLiveChatClient();
        // VK Video Live events
        _chatClient.MessageReceived += async (_, e) =>
        {
            if (e.Message == null) return;
            Console.WriteLine($"[VkVideoLive] Message received from: {e.Message.Data?.Author?.DisplayName}");
            await signalRService.SendChatMessageAsync(UkiChatMessage.FromVkVideoLiveMessage(e.Message));
        };

        _chatClient.Connected += async (_, _) =>
        {
            Console.WriteLine("[VkVideoLive] Connected");
            await SendChatMessageNotification(string.Format(
                localizationService.GetString("vkvideolive.connectedToChannel"),
                _channelName));
        };

        _chatClient.Disconnected += async (_, e) =>
        {
            Console.WriteLine($"[VkVideoLive] Disconnected: {e.Reason}");
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("vkvideolive.disconnectedFromChannel"), _channelName));
        };

        _chatClient.Error += (_, e) => { Console.WriteLine($"[VkVideoLive] Error: {e.Message}"); };
    }

    public async Task ConnectAsync(VkVideoLiveConnectionParams connectionParams)
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
            //var accessToken = vkSettings.ApiAccessToken;
            var accessTokenResponse = await _vkVideoLiveApiService.GetAccessTokenAsync(vkSettings.ApiClientId, vkSettings.ApiClientSecret);
            var accessToken = accessTokenResponse.AccessToken;
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

            _channelName = vkSettings.Channel;
            await SendChatMessageNotification(string.Format(
                _localizationService.GetString("vkvideolive.connectingToChannel"), channelInfo.Data.Channel.Id));

            var wsTokenResponse = await _vkVideoLiveApiService.GetWebSocketTokenAsync(accessToken);
            // Подключаемся к чату
            await _chatClient.ConnectAsync(wsTokenResponse.Data.Token, channelInfo.Data.Channel.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VkVideoLive] Connection error: {ex.Message}");
            await SendChatMessageNotification(string.Format(
                _localizationService.GetString("vkvideolive.connectingToChannelError"),
                vkSettings.Channel));
        }
    }

    public async Task ChangeChannelAsync(string newChannel)
    {
        var vkVideoLiveSettings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
        var oldChannel = vkVideoLiveSettings.Channel;
        if (oldChannel == newChannel)
            return;
        
        _channelName = newChannel;
        UpdateVkVideoLiveDbSettings(vkVideoLiveSettings);
        await ConnectAsync(new VkVideoLiveConnectionParams());
    }

    public Task LoadGlobalDataAsync()
    {
        throw new NotImplementedException();
    }

    public Task LoadChannelDataAsync()
    {
        throw new NotImplementedException();
    }

    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
    
    private void UpdateVkVideoLiveDbSettings(VkVideoLiveSettings vkVideoLiveSettings)
    {
        vkVideoLiveSettings.Channel = _channelName;
        _databaseContext.VkVideoLiveSettingsRepository.Save(vkVideoLiveSettings);
    }
}