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
            new TwitchSettingsData(twitchSettings.Channel, twitchSettings.ShowStreamUptime),
            new VkVideoLiveSettingsData(vkVideoLiveSettings.Channel)
        );
    }

    public void UpdateTwitchSettings(TwitchSettingsData data)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        twitchSettings.Channel = data.Channel;
        twitchSettings.ShowStreamUptime = data.ShowStreamUptime;
        _databaseContext.TwitchSettingsRepository.Save(twitchSettings);
    }

    public void UpdateTwitchApiTokens(string accessToken, string refreshToken)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        twitchSettings.ApiAccessToken = accessToken;
        twitchSettings.ApiRefreshToken = refreshToken;
        _databaseContext.TwitchSettingsRepository.Save(twitchSettings);
    }

    public void UpdateTwitchUserTokens(string accessToken, string refreshToken, string userId, string login)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        twitchSettings.UserAccessToken = accessToken;
        twitchSettings.UserRefreshToken = refreshToken;
        twitchSettings.UserId = userId;
        twitchSettings.UserLogin = login;
        _databaseContext.TwitchSettingsRepository.Save(twitchSettings);
    }

    public void ClearTwitchUserAuth()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        twitchSettings.UserAccessToken = null;
        twitchSettings.UserRefreshToken = null;
        twitchSettings.UserId = null;
        twitchSettings.UserLogin = null;
        _databaseContext.TwitchSettingsRepository.Save(twitchSettings);
    }

    public TwitchAuthStatusData GetTwitchAuthStatus()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var authorized = !string.IsNullOrEmpty(twitchSettings.UserAccessToken);
        return new TwitchAuthStatusData(authorized, twitchSettings.UserLogin);
    }

    public void UpdateVkVideoLiveSettings(VkVideoLiveSettingsData data)
    {
        var vkVideoLiveSettings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
        vkVideoLiveSettings.Channel = data.Channel;
        _databaseContext.VkVideoLiveSettingsRepository.Save(vkVideoLiveSettings);
    }

    public void UpdateVkVideoLiveTokens(string apiAccessToken, string wsAccessToken)
    {
        var vkVideoLiveSettings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
        vkVideoLiveSettings.ApiAccessToken = apiAccessToken;
        vkVideoLiveSettings.WsAccessToken = wsAccessToken;
        _databaseContext.VkVideoLiveSettingsRepository.Save(vkVideoLiveSettings);
    }

    public void UpdateDonationAlertsUserTokens(string accessToken, string refreshToken, string userId, string userName)
    {
        var settings = _databaseContext.DonationAlertsSettingsRepository.GetActiveSettings();
        settings.AccessToken = accessToken;
        settings.RefreshToken = refreshToken;
        settings.UserId = userId;
        settings.UserName = userName;
        _databaseContext.DonationAlertsSettingsRepository.Save(settings);
    }

    public void ClearDonationAlertsUserAuth()
    {
        var settings = _databaseContext.DonationAlertsSettingsRepository.GetActiveSettings();
        settings.AccessToken = null;
        settings.RefreshToken = null;
        settings.UserId = null;
        settings.UserName = null;
        _databaseContext.DonationAlertsSettingsRepository.Save(settings);
    }

    public DonationAlertsAuthStatusData GetDonationAlertsAuthStatus()
    {
        var settings = _databaseContext.DonationAlertsSettingsRepository.GetActiveSettings();
        var authorized = !string.IsNullOrEmpty(settings.AccessToken);
        return new DonationAlertsAuthStatusData(authorized, settings.UserName);
    }

    public ScaleSettingsData GetScaleSettings()
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        return new ScaleSettingsData(
            appSettings.Appearance.Main.Scale,
            appSettings.Appearance.Overlay.Scale
        );
    }

    public void UpdateScaleSettings(ScaleSettingsData data)
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        appSettings.Appearance.Main.Scale = data.MainWindowScale;
        appSettings.Appearance.Overlay.Scale = data.OverlayScale;
        _databaseContext.AppSettingsRepository.Save(appSettings);
    }

    public ThemeSettingsData GetThemeSettings()
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        return new ThemeSettingsData(
            appSettings.Appearance.Main.Theme,
            appSettings.Appearance.Overlay.Theme
        );
    }

    public void UpdateThemeSettings(ThemeSettingsData data)
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        appSettings.Appearance.Main.Theme = data.MainWindowTheme;
        appSettings.Appearance.Overlay.Theme = data.OverlayTheme;
        _databaseContext.AppSettingsRepository.Save(appSettings);
    }

    public MessageHideSettingsData GetMessageHideSettings()
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        return new MessageHideSettingsData(
            appSettings.Appearance.Main.MessageHideDelay,
            appSettings.Appearance.Overlay.MessageHideDelay
        );
    }

    public void UpdateMessageHideSettings(MessageHideSettingsData data)
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        appSettings.Appearance.Main.MessageHideDelay = data.MainWindowMessageHideDelay;
        appSettings.Appearance.Overlay.MessageHideDelay = data.OverlayMessageHideDelay;
        _databaseContext.AppSettingsRepository.Save(appSettings);
    }

    public ClipSettingsData GetClipSettings()
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        return new ClipSettingsData(appSettings.Appearance.Overlay.HideClippedMessages);
    }

    public void UpdateClipSettings(ClipSettingsData data)
    {
        var appSettings = _databaseContext.AppSettingsRepository.GetActiveAppSettings();
        appSettings.Appearance.Overlay.HideClippedMessages = data.OverlayHideClippedMessages;
        _databaseContext.AppSettingsRepository.Save(appSettings);
    }
}