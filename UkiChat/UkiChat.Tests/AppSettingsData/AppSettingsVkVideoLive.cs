namespace UkiChat.Tests.AppSettingsData;

public record AppSettingsVkVideoLive
{
    public AppSettingsVkVideoLiveChat Chat { get; init; }
    public AppSettingsVkVideoLiveApi Api { get; init; }
}