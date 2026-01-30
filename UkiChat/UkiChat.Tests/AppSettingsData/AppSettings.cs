namespace UkiChat.Tests.AppSettingsData;

public record AppSettings
{
    public AppSettingsTwitch Twitch { get; init; }
    public AppSettingsVkVideoLiveApi VkVideoLiveApi { get; init; }
}