namespace UkiChat.Data.AppSettingsData;

public record AppSettingsDatabase
{
    public string Filename { get; init; }
    public string Password { get; init; }
}