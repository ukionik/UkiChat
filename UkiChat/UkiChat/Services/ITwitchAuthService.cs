using System.Threading.Tasks;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public interface ITwitchAuthService
{
    /// <summary>
    /// Строит URL авторизации Twitch (Authorization Code flow) с нужными scope и state.
    /// Возвращает пустую строку, если не настроен ApiClientId.
    /// </summary>
    string BuildAuthorizeUrl();

    /// <summary>
    /// Обрабатывает редирект-callback: обменивает code на токены, сохраняет их и уведомляет фронт.
    /// </summary>
    /// <returns>true при успехе.</returns>
    Task<bool> HandleCallbackAsync(string code, string state);

    /// <summary>
    /// Сбрасывает авторизацию пользователя.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Текущий статус авторизации.
    /// </summary>
    TwitchAuthStatusData GetStatus();
}
