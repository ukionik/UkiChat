using LiteDB;

namespace UkiChat.Entities;

public class TwitchSettings : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }

    public string? Channel { get; set; }

    // Реквизиты приложения с dev.twitch.tv — приезжают из вшитого app-settings.local.toml.
    public string? ApiClientId { get; set; }
    public string? ApiClientSecret { get; set; }
    public string? ApiBroadcasterId { get; set; }

    // Токены авторизованного пользователя (OAuth Authorization Code flow) — ЕДИНСТВЕННЫЕ
    // учётные данные, которыми работают и чат (IRC, scope chat:read), и Helix.
    // Появляются только после авторизации, вшитых значений для них нет.
    public string? UserAccessToken { get; set; }
    public string? UserRefreshToken { get; set; }
    public string? UserId { get; set; }
    public string? UserLogin { get; set; }

    public bool ShowStreamUptime { get; set; }

    [BsonRef]
    public required AppSettings? AppSettings { get; set; }
}