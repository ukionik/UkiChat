using UkiChat.Model.Info;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public interface IDatabaseService
{
    AppSettingsInfoData GetActiveAppSettingsInfo();
    AppSettingsData GetActiveAppSettingsData();
    ScaleSettingsData GetScaleSettings();
    ThemeSettingsData GetThemeSettings();
    void UpdateTwitchSettings(TwitchSettingsData data);
    void UpdateTwitchApiTokens(string accessToken, string refreshToken);
    void UpdateVkVideoLiveSettings(VkVideoLiveSettingsData data);
    void UpdateVkVideoLiveTokens(string apiAccessToken, string wsAccessToken);
    void UpdateScaleSettings(ScaleSettingsData data);
    void UpdateThemeSettings(ThemeSettingsData data);
}