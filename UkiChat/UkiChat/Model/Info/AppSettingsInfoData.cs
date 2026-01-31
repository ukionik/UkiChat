using UkiChat.Model.Settings;

namespace UkiChat.Model.Info;

public record AppSettingsInfoData(string ProfileName
    , string Language
    , TwitchSettingsInfo Twitch
    , VkVideoLiveSettingsInfo VkVideoLive);