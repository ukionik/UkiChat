using System;
using LiteDB;
using UkiChat.Data.AppSettingsData;
using UkiChat.Entities;
using UkiChat.Repositories;

namespace UkiChat.Configuration;

public class DatabaseContext : IDatabaseContext, IDisposable
{
    public DatabaseContext(string connectionString
        , AppSettingsTwitch appSettingsTwitch)
    {
        var db = new LiteDatabase(connectionString);
        TwitchGlobalSettingsRepository = new TwitchGlobalSettingsRepository(db);
        ProfileRepository = new ProfileRepository(db);
        InitDefaultData(appSettingsTwitch);
    }

    public ITwitchGlobalSettingsRepository TwitchGlobalSettingsRepository { get; }
    public IProfileRepository ProfileRepository { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private void InitDefaultData(AppSettingsTwitch appSettingsTwitch)
    {
        InitTwitchGlobalSettings(appSettingsTwitch);
        InitDefaultProfile();
    }

    private void InitTwitchGlobalSettings(AppSettingsTwitch appSettingsTwitch)
    {
        var twitchGlobalSettings = TwitchGlobalSettingsRepository.Get();
        twitchGlobalSettings.TwitchChatBotUsername = appSettingsTwitch.ChatbotUsername;
        twitchGlobalSettings.TwitchChatBotAccessToken = appSettingsTwitch.ChatbotAccessToken;
        TwitchGlobalSettingsRepository.Save(twitchGlobalSettings);
    }

    private void InitDefaultProfile()
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
    }
}