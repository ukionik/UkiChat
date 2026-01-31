namespace UkiChat.Data.DefaultAppSettingsData;

public record DefaultAppSettings
{
    public DefaultAppSettingsDatabase Database { get; init; }
    public DefaultAppSettingsTwitch Twitch { get; init; }
    public DefaultAppSettingsVkVideoLive VkVideoLive { get; init; }
}