namespace UkiChat.Data.DefaultAppSettingsData;

public record DefaultAppSettingsDatabase
{
    public string Filename { get; init; }
    public string Password { get; init; }
}