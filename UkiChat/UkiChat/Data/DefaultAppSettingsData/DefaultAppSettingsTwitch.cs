namespace UkiChat.Data.DefaultAppSettingsData;

public record DefaultAppSettingsTwitch
{
    public string ApiClientId { get; init; }
    public string ApiClientSecret { get; init; }
    public string ApiRefreshToken { get; init; }
    public string ChatbotUsername { get; init; }
    public string ChatbotAccessToken { get; init; }
}