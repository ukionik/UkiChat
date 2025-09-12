using System;
using LiteDB;
using UkiChat.Data.AppSettingsData;
using UkiChat.Repositories;

namespace UkiChat.Configuration;

public class DatabaseContext : IDatabaseContext, IDisposable
{
    private readonly LiteDatabase _db;

    public DatabaseContext(string connectionString
        , AppSettingsTwitch appSettingsTwitch)
    {
        _db = new LiteDatabase(connectionString);
        TwitchGlobalSettingsRepository = new TwitchGlobalSettingsRepository(_db);
        InitDefaultData(appSettingsTwitch);
    }

    private void InitDefaultData(AppSettingsTwitch appSettingsTwitch)
    {
        var twitchGlobalSettings = TwitchGlobalSettingsRepository.Get();
        twitchGlobalSettings.TwitchChatBotUsername = appSettingsTwitch.ChatbotUsername;
        twitchGlobalSettings.TwitchChatBotAccessToken = appSettingsTwitch.ChatbotAccessToken;
        TwitchGlobalSettingsRepository.Save(twitchGlobalSettings);
    }

    public ITwitchGlobalSettingsRepository TwitchGlobalSettingsRepository { get; }

    public void Dispose()
    {
        _db.Dispose();
    }
}