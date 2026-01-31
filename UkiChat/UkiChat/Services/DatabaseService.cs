using UkiChat.Configuration;
using UkiChat.Model.Info;
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
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var vkVideoLiveSettings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
        return new AppSettingsInfoData(
            appSettings.Profile.Name,
            appSettings.Language,
            new TwitchSettingsInfo(twitchSettings.Channel),
            new VkVideoLiveSettingsInfo(vkVideoLiveSettings.Channel)
        );
    }

    public AppSettingsData GetActiveAppSettingsData()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var vkVideoLiveSettings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
        return new AppSettingsData(
            new TwitchSettingsData(twitchSettings.Channel),
            new VkVideoLiveSettingsData(vkVideoLiveSettings.Channel)
        );
    }

    public void UpdateTwitchSettings(TwitchSettingsData data)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        twitchSettings.Channel = data.Channel;
        _databaseContext.TwitchSettingsRepository.Save(twitchSettings);
    }

    public void UpdateTwitchApiTokens(string accessToken, string refreshToken)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        twitchSettings.ApiAccessToken = accessToken;
        twitchSettings.ApiRefreshToken = refreshToken;
        _databaseContext.TwitchSettingsRepository.Save(twitchSettings);
    }

    public void UpdateVkVideoLiveSettings(VkVideoLiveSettingsData data)
    {
        var vkVideoLiveSettings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
        vkVideoLiveSettings.Channel = data.Channel;
        _databaseContext.VkVideoLiveSettingsRepository.Save(vkVideoLiveSettings);
    }
}