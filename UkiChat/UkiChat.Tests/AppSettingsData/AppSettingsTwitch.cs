namespace UkiChat.Tests.AppSettingsData;

public record AppSettingsTwitch
{
    public AppSettingsTwitchApi Api { get; init; }
    public AppSettingsTwitchChat Chat { get; init; }
};