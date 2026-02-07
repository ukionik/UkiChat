using System;
using System.Threading.Tasks;
using UkiChat.Configuration;
using UkiChat.Entities;
using UkiChat.Repositories.Memory;

namespace UkiChat.Services;

public class AppInitializationService(
    ILocalizationService localizationService,
    IDatabaseContext databaseContext,
    IDatabaseService databaseService,
    TwitchApiService twitchApiService,
    TwitchBadgesRepository twitchBadgesRepository) : IAppInitializationService
{
    public async Task InitializeAsync()
    {
        localizationService.SetCulture("ru");
        await LoadTwitchDataAsync();
    }

    public async Task LoadTwitchChannelDataAsync(string channelName)
    {
        await LoadTwitchChannelBadgesAsync(channelName);
    }

    private async Task LoadTwitchDataAsync()
    {
        var twitchSettings = databaseContext.TwitchSettingsRepository.GetActiveSettings();
        await InitializeTwitchApiAsync(twitchSettings);
        await LoadTwitchGlobalBadgesAsync();
        await LoadTwitchChannelDataAsync(twitchSettings.Channel);
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

        await twitchApiService.InitializeAsync(twitchSettings.ApiClientId, twitchSettings.ApiAccessToken);

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

    private async Task LoadTwitchGlobalBadgesAsync()
    {
        try
        {
            var twitchGlobalBadges = await twitchApiService.GetGlobalChatBadgesAsync();
            twitchBadgesRepository.SetGlobalBadges(twitchGlobalBadges);
            Console.WriteLine($"Loaded {twitchGlobalBadges.EmoteSet.Length} global badge sets");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading twitch global badges: {e.Message}");
        }
    }

    private async Task LoadTwitchChannelBadgesAsync(string channelName)
    {
        try
        {
            // Загружаем бейджи канала (если есть имя канала)
            if (!string.IsNullOrEmpty(channelName))
            {
                var broadcasterId = await twitchApiService.GetBroadcasterIdAsync(channelName);
                if (!string.IsNullOrEmpty(broadcasterId))
                {
                    var channelBadges = await twitchApiService.GetChannelChatBadgesAsync(broadcasterId);
                    twitchBadgesRepository.SetChannelBadges(broadcasterId, channelBadges);
                    Console.WriteLine($"Loaded {channelBadges.EmoteSet.Length} channel badge sets for {channelName}");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading twitch chat badges: {e.Message}");
        }
    }
}