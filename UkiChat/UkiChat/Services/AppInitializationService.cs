using System;
using System.Threading.Tasks;
using UkiChat.Configuration;
using UkiChat.Entities;
using UkiChat.Model.Twitch;
using UkiChat.Model.VkVideoLive;

namespace UkiChat.Services;

public class AppInitializationService(
    ILocalizationService localizationService,
    IDatabaseContext databaseContext,
    IDatabaseService databaseService,
    ITwitchChatService twitchChatService,
    ITwitchApiService twitchApiService,
    IVkVideoLiveChatService vkVideoLiveChatService,
    IVkVideoLiveApiService vkVideoLiveApiService
    ) : IAppInitializationService
{
    public async Task InitializeAsync()
    {
        localizationService.SetCulture("ru");
        await LoadTwitchDataAsync();
        await LoadVkVideoLiveDataAsync();
    }

    private async Task LoadTwitchDataAsync()
    {
        var twitchSettings = databaseContext.TwitchSettingsRepository.GetActiveSettings();
        await InitializeTwitchApiAsync(twitchSettings);
        await twitchChatService.LoadGlobalDataAsync();
        await twitchChatService.LoadChannelDataAsync();
        await twitchChatService.ConnectAsync(
            TwitchConnectionParams.OfTwitchSettings("", twitchSettings.Channel ?? "", twitchSettings));
    }

    private async Task InitializeTwitchApiAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.ApiClientId))
        {
            Console.WriteLine("Twitch API client id is null or empty");
            return;
        }

        if (string.IsNullOrEmpty(twitchSettings.ApiClientSecret))
        {
            Console.WriteLine("Twitch API client secret is null or empty");
            return;
        }

        await twitchApiService.InitializeAsync(twitchSettings.ApiClientId, twitchSettings.ApiAccessToken ?? "");

        // Проверяем валидность токена и обновляем при необходимости
        await RefreshTwitchApiTokensAsync(twitchSettings);
    }

    private async Task RefreshTwitchApiTokensAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.ApiRefreshToken) ||
            string.IsNullOrEmpty(twitchSettings.ApiClientId) ||
            string.IsNullOrEmpty(twitchSettings.ApiClientSecret))
            return;

        var newTokens = await twitchApiService.EnsureValidTokenAsync(
            twitchSettings.ApiRefreshToken,
            twitchSettings.ApiClientId,
            twitchSettings.ApiClientSecret);

        if (newTokens != null)
        {
            databaseService.UpdateTwitchApiTokens(newTokens.AccessToken, newTokens.RefreshToken);
            Console.WriteLine("Twitch API tokens refreshed");
        }
    }

    private async Task LoadVkVideoLiveDataAsync()
    {
        var vkSettings = databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();

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

        var connectionParams = await InitializeVkVideoLiveApiAsync(vkSettings);
        if (connectionParams == null) return;

        await vkVideoLiveChatService.ConnectAsync(connectionParams);
    }

    private async Task<VkVideoLiveConnectionParams?> InitializeVkVideoLiveApiAsync(VkVideoLiveSettings vkSettings)
    {
        // Проверяем валидность токена и обновляем при необходимости
        await RefreshVkVideoLiveApiTokenAsync(vkSettings);
        var apiAccessToken = vkSettings.ApiAccessToken!;

        // Получаем информацию о канале
        var channelInfo = await vkVideoLiveApiService.GetChannelInfoAsync(apiAccessToken, vkSettings.Channel!);
        var channelId = channelInfo.Data.Channel.Id;

        // Получаем WsAccessToken
        var wsTokenResponse = await vkVideoLiveApiService.GetWebSocketTokenAsync(apiAccessToken);
        var wsAccessToken = wsTokenResponse.Data.Token;
        Console.WriteLine("[VkVideoLive] WebSocket token received");

        // Сохраняем токены в базу
        databaseService.UpdateVkVideoLiveTokens(apiAccessToken, wsAccessToken);

        return new VkVideoLiveConnectionParams(
            OldChannelName: "",
            ChannelName: vkSettings.Channel!,
            ChannelId: channelId,
            WsAccessToken: wsAccessToken);
    }

    private async Task RefreshVkVideoLiveApiTokenAsync(VkVideoLiveSettings vkSettings)
    {
        if (string.IsNullOrEmpty(vkSettings.ApiAccessToken))
        {
            await FetchVkVideoLiveApiTokenAsync(vkSettings);
            return;
        }

        // Проверяем валидность существующего токена
        try
        {
            var tokenInfo = await vkVideoLiveApiService.ValidateAccessTokenAsync(vkSettings.ApiAccessToken);
            var expiredAt = DateTimeOffset.FromUnixTimeSeconds(tokenInfo.Data.ExpiredAt);
            if (expiredAt <= DateTimeOffset.UtcNow)
            {
                Console.WriteLine("[VkVideoLive] API access token expired, fetching new one");
                await FetchVkVideoLiveApiTokenAsync(vkSettings);
            }
            else
            {
                Console.WriteLine("[VkVideoLive] API access token is valid");
            }
        }
        catch
        {
            Console.WriteLine("[VkVideoLive] API access token validation failed, fetching new one");
            await FetchVkVideoLiveApiTokenAsync(vkSettings);
        }
    }

    private async Task FetchVkVideoLiveApiTokenAsync(VkVideoLiveSettings vkSettings)
    {
        var tokenResponse = await vkVideoLiveApiService.GetAccessTokenAsync(
            vkSettings.ApiClientId!, vkSettings.ApiClientSecret!);
        vkSettings.ApiAccessToken = tokenResponse.AccessToken;
        Console.WriteLine("[VkVideoLive] API access token refreshed");
    }
}