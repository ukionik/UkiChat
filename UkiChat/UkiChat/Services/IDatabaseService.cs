using UkiChat.Entities;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public interface IDatabaseService
{
    void UpdateTwitchSettings(TwitchSettingsData data);
    AppSettingsInfoData GetActiveAppSettingsInfo();
}