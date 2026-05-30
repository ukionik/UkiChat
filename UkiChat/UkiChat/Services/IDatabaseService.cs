using UkiChat.Model.Info;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public interface IDatabaseService
{
    AppSettingsInfoData GetActiveAppSettingsInfo();
    AppSettingsData GetActiveAppSettingsData();
    ScaleSettingsData GetScaleSettings();
    ThemeSettingsData GetThemeSettings();
    MessageHideSettingsData GetMessageHideSettings();
    ClipSettingsData GetClipSettings();
    void UpdateTwitchSettings(TwitchSettingsData data);
    void UpdateTwitchApiTokens(string accessToken, string refreshToken);
    void UpdateTwitchUserTokens(string accessToken, string refreshToken, string userId, string login);
    void ClearTwitchUserAuth();
    TwitchAuthStatusData GetTwitchAuthStatus();
    void UpdateVkVideoLiveSettings(VkVideoLiveSettingsData data);
    void UpdateVkVideoLiveTokens(string apiAccessToken, string wsAccessToken);
    void UpdateDonationAlertsUserTokens(string accessToken, string refreshToken, string userId, string userName);
    void ClearDonationAlertsUserAuth();
    DonationAlertsAuthStatusData GetDonationAlertsAuthStatus();
    void UpdateScaleSettings(ScaleSettingsData data);
    void UpdateThemeSettings(ThemeSettingsData data);
    void UpdateMessageHideSettings(MessageHideSettingsData data);
    void UpdateClipSettings(ClipSettingsData data);
    MentionSettingsData GetMentionSettings();
    void UpdateMentionSettings(MentionSettingsData data);
}