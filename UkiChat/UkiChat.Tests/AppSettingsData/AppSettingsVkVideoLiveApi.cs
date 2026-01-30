namespace UkiChat.Tests.AppSettingsData;

public record AppSettingsVkVideoLiveApi
{
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
}