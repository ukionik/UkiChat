namespace UkiChat.Tests.AppSettingsData;

public record AppSettingsTwitchApi
{
    public string Code { get; init; }
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
};