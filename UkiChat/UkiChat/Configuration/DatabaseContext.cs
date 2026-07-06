using System;
using System.Globalization;
using LiteDB;
using UkiChat.Data.DefaultAppSettingsData;
using UkiChat.Entities;
using UkiChat.Repositories.Database;

namespace UkiChat.Configuration;

public class DatabaseContext : IDatabaseContext, IDisposable
{
    public DatabaseContext(string connectionString
        , DefaultAppSettings defaultAppSettings)
    {
        var db = new LiteDatabase(connectionString);
        ProfileRepository = new ProfileRepository(db);
        AppSettingsRepository = new AppSettingsRepository(db);
        TwitchSettingsRepository = new TwitchSettingsRepository(db);
        VkVideoLiveSettingsRepository = new VkVideoLiveSettingsRepository(db);
        YouTubeSettingsRepository = new YouTubeSettingsRepository(db);
        DonationAlertsSettingsRepository = new DonationAlertsSettingsRepository(db);
        SevenTvEmoteRepository = new SevenTvEmoteRepository(db);
        FfzEmoteRepository = new FfzEmoteRepository(db);
        BttvEmoteRepository = new BttvEmoteRepository(db);
        TwitchBadgeRepository = new TwitchBadgeRepository(db);
        InitDefaultData(defaultAppSettings);
    }

    public ITwitchSettingsRepository TwitchSettingsRepository { get; }
    public IVkVideoLiveSettingsRepository VkVideoLiveSettingsRepository { get; }
    public IYouTubeSettingsRepository YouTubeSettingsRepository { get; }
    public IDonationAlertsSettingsRepository DonationAlertsSettingsRepository { get; }
    public IProfileRepository ProfileRepository { get; }
    public IAppSettingsRepository AppSettingsRepository { get; }
    public ISevenTvEmoteRepository SevenTvEmoteRepository { get; }
    public IFfzEmoteRepository FfzEmoteRepository { get; }
    public IBttvEmoteRepository BttvEmoteRepository { get; }
    public ITwitchBadgeRepository TwitchBadgeRepository { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private void InitDefaultData(DefaultAppSettings defaultAppSettings)
    {
        InitDefaultProfile(defaultAppSettings);
        MigrateData(defaultAppSettings);
        MigrateYouTubeData();
    }

    // Миграция для БД, созданных до добавления YouTubeSettings
    private void MigrateYouTubeData()
    {
        if (YouTubeSettingsRepository.GetActiveSettings() != null)
            return;

        var appSettings = AppSettingsRepository.GetActiveAppSettings();
        if (appSettings == null)
            return;

        YouTubeSettingsRepository.Save(new YouTubeSettings { AppSettings = appSettings });
    }

    // Миграция для БД, созданных до добавления DonationAlertsSettings
    private void MigrateData(DefaultAppSettings defaultAppSettings)
    {
        if (DonationAlertsSettingsRepository.GetActiveSettings() != null)
            return;

        var appSettings = AppSettingsRepository.GetActiveAppSettings();
        if (appSettings == null)
            return;

        DonationAlertsSettingsRepository.Save(new DonationAlertsSettings
        {
            ClientId = defaultAppSettings.DonationAlerts.ClientId,
            ClientSecret = defaultAppSettings.DonationAlerts.ClientSecret,
            AppSettings = appSettings
        });
    }

    private void InitDefaultProfile(DefaultAppSettings defaultAppSettings)
    {
        if (ProfileRepository.Count() != 0)
            return;

        var profile = new Profile
        {
            Name = "Default",
            Active = true,
            Default = true
        };
        ProfileRepository.Save(profile);
        InitDefaultSettings(profile, defaultAppSettings);
    }

    private void InitDefaultSettings(Profile profile, DefaultAppSettings defaultAppSettings)
    {
        var appSettings = new AppSettings
        {
            Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            Profile = profile
        };
        AppSettingsRepository.Save(appSettings);
        var twitchSettings = new TwitchSettings
        {
            ChatbotUsername = defaultAppSettings.Twitch.ChatbotUsername,
            ChatbotAccessToken = defaultAppSettings.Twitch.ChatbotAccessToken,
            ApiClientId = defaultAppSettings.Twitch.ApiClientId,
            ApiClientSecret = defaultAppSettings.Twitch.ApiClientSecret,
            ApiRefreshToken = defaultAppSettings.Twitch.ApiRefreshToken,
            AppSettings = appSettings,
        };
        TwitchSettingsRepository.Save(twitchSettings);

        var vkVideoLiveSettings = new VkVideoLiveSettings
        {
            ApiClientId = defaultAppSettings.VkVideoLive.ApiClientId,
            ApiClientSecret = defaultAppSettings.VkVideoLive.ApiClientSecret,
            AppSettings = appSettings
        };
        VkVideoLiveSettingsRepository.Save(vkVideoLiveSettings);

        var youTubeSettings = new YouTubeSettings
        {
            AppSettings = appSettings
        };
        YouTubeSettingsRepository.Save(youTubeSettings);

        var donationAlertsSettings = new DonationAlertsSettings
        {
            ClientId = defaultAppSettings.DonationAlerts.ClientId,
            ClientSecret = defaultAppSettings.DonationAlerts.ClientSecret,
            AppSettings = appSettings
        };
        DonationAlertsSettingsRepository.Save(donationAlertsSettings);
    }
}