namespace UkiChat.Data.AppSettingsData;

public record AppSettings
{
    public AppSettingsDatabase Database { get; init; }
    public AppSettingsTwitch Twitch { get; init; }
}