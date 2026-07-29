namespace UkiChat.Data.DefaultAppSettingsData;

/// <summary>
///     Дефолты Twitch из вшитого app-settings.local.toml. Здесь остались ТОЛЬКО реквизиты
///     приложения: ни токена чат-бота, ни refresh-токена аккаунта тут больше нет — и то и другое
///     давало доступ к живому аккаунту с первой же копии exe. Чат и Helix ходят токеном
///     пользователя, который выдаётся OAuth-авторизацией и живёт только в локальной БД.
/// </summary>
public record DefaultAppSettingsTwitch
{
    public string ApiClientId { get; init; }
    public string ApiClientSecret { get; init; }
}