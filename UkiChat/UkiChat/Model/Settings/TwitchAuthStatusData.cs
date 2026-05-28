namespace UkiChat.Model.Settings;

/// <summary>
/// Статус авторизации пользователя Twitch.
/// </summary>
public record TwitchAuthStatusData(bool Authorized, string? Login);
