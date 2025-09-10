namespace UkiChat.Tests.AppSettingsData;

public record AppSettingsTwitchChat
{
    public string Username { get; init; }
    public string AccessToken { get; init; }
    public string Channel { get; init; }
    public string ClientId { get; init; }
    public string RefreshToken { get; init; }
}