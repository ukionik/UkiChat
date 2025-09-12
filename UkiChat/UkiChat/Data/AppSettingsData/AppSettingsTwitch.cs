namespace UkiChat.Data.AppSettingsData;

public record AppSettingsTwitch
{
    public string ChatbotUsername { get; init; }
    public string ChatbotAccessToken { get; init; }
}