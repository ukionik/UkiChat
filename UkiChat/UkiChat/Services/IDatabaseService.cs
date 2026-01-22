using UkiChat.Model.Info;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public interface IDatabaseService
{
    AppSettingsInfoData GetActiveAppSettingsInfo();
    AppSettingsData GetActiveAppSettingsData();
    void UpdateTwitchSettings(TwitchSettingsData data);
    void UpdateTwitchApiTokens(string accessToken, string refreshToken);
}