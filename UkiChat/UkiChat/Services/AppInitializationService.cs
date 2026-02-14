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
    IVkVideoLiveChatService vkVideoLiveChatService
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
        await vkVideoLiveChatService.ConnectAsync(new VkVideoLiveConnectionParams());
    }
}