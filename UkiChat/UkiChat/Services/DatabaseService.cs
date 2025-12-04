using System;
using UkiChat.Configuration;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public class DatabaseService : IDatabaseService
{
    private readonly IDatabaseContext _databaseContext;

    public DatabaseService(IDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public void UpdateTwitchSettings(TwitchSettingsData data)
    {
        Console.WriteLine("Updating twitch settings data");
    }

    public AppSettingsInfoData GetActiveAppSettingsInfo()
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        return new AppSettingsInfoData(appSettings.Profile.Name, appSettings.Language);
    }
}