namespace UkiChat.Tests.AppSettingsData;

public record AppSettings
{
    public AppSettingsTwitch Twitch { get; init; }
    public AppSettingsVkVideoLive VkVideoLive { get; init; }
}