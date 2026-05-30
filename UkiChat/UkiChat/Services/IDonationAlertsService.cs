using System.Threading.Tasks;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public interface IDonationAlertsService
{
    /// <summary>Строит URL авторизации DonationAlerts (Authorization Code flow).</summary>
    string BuildAuthorizeUrl();

    /// <summary>Обрабатывает OAuth callback: обмен кода на токены, сохранение, подключение.</summary>
    Task<bool> HandleCallbackAsync(string code, string state);

    /// <summary>Сбрасывает авторизацию и отключается от Centrifugo.</summary>
    Task LogoutAsync();

    /// <summary>Текущий статус авторизации.</summary>
    DonationAlertsAuthStatusData GetStatus();

    /// <summary>Подключается к Centrifugo, если пользователь авторизован.</summary>
    Task ConnectAsync();

    /// <summary>Отключается от Centrifugo.</summary>
    Task DisconnectAsync();
}
