using System.Threading.Tasks;
using UkiChat.Model.DonationAlerts;

namespace UkiChat.Services;

public interface IDonationAlertsApiService
{
    /// <summary>Обменивает authorization code на access/refresh токены.</summary>
    Task<DonationAlertsTokenResponse> ExchangeCodeForTokensAsync(string code, string clientId, string clientSecret, string redirectUri);

    /// <summary>Обновляет токены по refresh_token.</summary>
    Task<DonationAlertsTokenResponse> RefreshTokensAsync(string refreshToken, string clientId, string clientSecret);

    /// <summary>Данные пользователя + socket_connection_token для Centrifugo.</summary>
    Task<DonationAlertsUserResponse> GetUserAsync(string accessToken);

    /// <summary>Получает per-channel токен подписки на приватный канал Centrifugo.</summary>
    Task<string?> GetCentrifugeSubscribeTokenAsync(string accessToken, string clientId, string channel);
}
