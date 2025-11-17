using System;
using System.Globalization;
using LiteDB;
using UkiChat.Data.DefaultAppSettingsData;
using UkiChat.Entities;
using UkiChat.Repositories;

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
        InitDefaultData(defaultAppSettings);
    }

    public ITwitchSettingsRepository TwitchSettingsRepository { get; }
    public IProfileRepository ProfileRepository { get; }
    public IAppSettingsRepository AppSettingsRepository { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private void InitDefaultData(DefaultAppSettings defaultAppSettings)
    {
        InitDefaultProfile(defaultAppSettings);
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
            AppSettings = appSettings
        };
        TwitchSettingsRepository.Save(twitchSettings);
    }
}