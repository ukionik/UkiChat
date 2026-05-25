using System;
using System.Threading.Tasks;
using UkiChat.Configuration;
using UkiChat.Diagnostics;
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
        StartupDiagnostics.Log("app-init", "InitializeAsync: BEGIN");
        using (StartupDiagnostics.Measure("app-init", "SetCulture(ru)"))
        {
            localizationService.SetCulture("ru");
        }

        using (StartupDiagnostics.Measure("app-init", "LoadTwitchData + LoadVkVideoLiveData (parallel)"))
        {
            await Task.WhenAll(LoadTwitchDataAsync(),
                LoadVkVideoLiveDataAsync());
        }
        StartupDiagnostics.Log("app-init", "InitializeAsync: END");
    }

    private async Task LoadTwitchDataAsync()
    {
        using var _ = StartupDiagnostics.Measure("app-init", "LoadTwitchDataAsync");
        TwitchSettings twitchSettings;
        using (StartupDiagnostics.Measure("app-init", "  read TwitchSettings from DB"))
        {
            twitchSettings = databaseContext.TwitchSettingsRepository.GetActiveSettings();
        }
        StartupDiagnostics.Log("app-init",
            $"  Twitch channel={twitchSettings.Channel ?? "<none>"} hasApi={!string.IsNullOrEmpty(twitchSettings.ApiClientId)}");

        using (StartupDiagnostics.Measure("app-init", "  InitializeTwitchApi"))
        {
            await InitializeTwitchApiAsync(twitchSettings);
        }

        using (StartupDiagnostics.Measure("app-init", "  Twitch parallel: LoadGlobalData + LoadChannelData + ConnectAsync"))
        {
            await Task.WhenAll(
                twitchChatService.LoadGlobalDataAsync(),
                twitchChatService.LoadChannelDataAsync(),
                twitchChatService.ConnectAsync(
                    TwitchConnectionParams.OfTwitchSettings("", twitchSettings.Channel ?? "", twitchSettings))
            );
        }
    }

    private async Task InitializeTwitchApiAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.ApiClientId))
        {
            StartupDiagnostics.Log("app-init", "Twitch API client id is null or empty");
            return;
        }

        if (string.IsNullOrEmpty(twitchSettings.ApiClientSecret))
        {
            StartupDiagnostics.Log("app-init", "Twitch API client secret is null or empty");
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
            StartupDiagnostics.Log("app-init", "Twitch API tokens refreshed");
        }
    }

    private async Task LoadVkVideoLiveDataAsync()
    {
        using var _ = StartupDiagnostics.Measure("app-init", "LoadVkVideoLiveDataAsync");
        var vkSettings = databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
        StartupDiagnostics.Log("app-init",
            $"  VK channel={vkSettings.Channel ?? "<none>"} hasApi={!string.IsNullOrEmpty(vkSettings.ApiClientId)}");

        if (string.IsNullOrEmpty(vkSettings.ApiClientId) || string.IsNullOrEmpty(vkSettings.ApiClientSecret))
        {
            StartupDiagnostics.Log("app-init", "[VkVideoLive] API credentials not configured");
            return;
        }

        using (StartupDiagnostics.Measure("app-init", "  InitializeVkVideoLiveApi"))
        {
            await InitializeVkVideoLiveApiAsync(vkSettings);
        }

        using (StartupDiagnostics.Measure("app-init", "  VkVideoLive ConnectAsync"))
        {
            await vkVideoLiveChatService.ConnectAsync(
                VkVideoLiveConnectionParams.OfVkVideoLiveSettings("", vkSettings.Channel ?? "", vkSettings));
        }
    }

    private async Task InitializeVkVideoLiveApiAsync(VkVideoLiveSettings vkSettings)
    {
        // Проверяем валидность токена и обновляем при необходимости
        await RefreshVkVideoLiveApiTokenAsync(vkSettings);
        var apiAccessToken = vkSettings.ApiAccessToken!;

        // Получаем WsAccessToken
        using (StartupDiagnostics.Measure("app-init", "    GetWebSocketTokenAsync"))
        {
            var wsTokenResponse = await vkVideoLiveApiService.GetWebSocketTokenAsync(apiAccessToken);
            var wsAccessToken = wsTokenResponse.Data.Token;
            StartupDiagnostics.Log("app-init", "[VkVideoLive] WebSocket token received");
            databaseService.UpdateVkVideoLiveTokens(apiAccessToken, wsAccessToken);
        }
    }

    private async Task RefreshVkVideoLiveApiTokenAsync(VkVideoLiveSettings vkSettings)
    {
        if (string.IsNullOrEmpty(vkSettings.ApiAccessToken))
        {
            await FetchVkVideoLiveApiTokenAsync(vkSettings);
            return;
        }

        try
        {
            using (StartupDiagnostics.Measure("app-init", "    ValidateAccessTokenAsync (VK)"))
            {
                var tokenInfo = await vkVideoLiveApiService.ValidateAccessTokenAsync(vkSettings.ApiAccessToken);
                var expiredAt = DateTimeOffset.FromUnixTimeSeconds(tokenInfo.Data.ExpiredAt);
                if (expiredAt <= DateTimeOffset.UtcNow)
                {
                    StartupDiagnostics.Log("app-init", "[VkVideoLive] API access token expired, fetching new one");
                    await FetchVkVideoLiveApiTokenAsync(vkSettings);
                }
                else
                {
                    StartupDiagnostics.Log("app-init", "[VkVideoLive] API access token is valid");
                }
            }
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app-init", "[VkVideoLive] API access token validation failed, fetching new one", ex);
            await FetchVkVideoLiveApiTokenAsync(vkSettings);
        }
    }

    private async Task FetchVkVideoLiveApiTokenAsync(VkVideoLiveSettings vkSettings)
    {
        using (StartupDiagnostics.Measure("app-init", "    GetAccessTokenAsync (VK)"))
        {
            var tokenResponse = await vkVideoLiveApiService.GetAccessTokenAsync(
                vkSettings.ApiClientId!, vkSettings.ApiClientSecret!);
            vkSettings.ApiAccessToken = tokenResponse.AccessToken;
            StartupDiagnostics.Log("app-init", "[VkVideoLive] API access token refreshed");
        }
    }
}
