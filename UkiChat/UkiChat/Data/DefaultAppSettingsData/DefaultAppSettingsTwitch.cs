namespace UkiChat.Data.DefaultAppSettingsData;

public record DefaultAppSettingsTwitch
{
    public string ChatbotUsername { get; init; }
    public string ChatbotAccessToken { get; init; }
}