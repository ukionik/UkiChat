using LiteDB;

namespace UkiChat.Entities;

public class TwitchSettings : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }

    // Chat credentials
    public string? ChatbotUsername { get; set; }
    public string? ChatbotAccessToken { get; set; }
    public string? Channel { get; set; }

    // API credentials (дефолтные, токен приложения — без авторизации пользователя)
    public string? ApiClientId { get; set; }
    public string? ApiClientSecret { get; set; }
    public string? ApiAccessToken { get; set; }
    public string? ApiRefreshToken { get; set; }
    public string? ApiBroadcasterId { get; set; }

    // Токены авторизованного пользователя (OAuth Authorization Code flow).
    // Хранятся отдельно от дефолтных, чтобы не затирать токен приложения.
    public string? UserAccessToken { get; set; }
    public string? UserRefreshToken { get; set; }
    public string? UserId { get; set; }
    public string? UserLogin { get; set; }

    [BsonRef]
    public required AppSettings? AppSettings { get; set; }
}