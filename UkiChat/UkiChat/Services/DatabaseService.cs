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
    
    public AppSettingsInfoData GetActiveAppSettingsInfo()
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        return new AppSettingsInfoData(appSettings.Profile.Name, appSettings.Language);
    }

    public AppSettingsData GetActiveAppSettingsData()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        return new AppSettingsData(new TwitchSettingsData(twitchSettings.Channel));
    }

    public void UpdateTwitchSettings(TwitchSettingsData data)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        twitchSettings.Channel = data.Channel;
        _databaseContext.TwitchSettingsRepository.Save(twitchSettings);
    }
}