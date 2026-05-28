using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;

namespace UkiChat.Services;

public interface ITwitchApiService
{
    Task InitializeAsync(string clientId, string accessToken);
    Task<bool> ValidateAccessTokenAsync();
    Task<GetGlobalChatBadgesResponse> GetGlobalChatBadgesAsync();
    Task<GetChannelChatBadgesResponse> GetChannelChatBadgesAsync(string broadcasterId);
    Task<RefreshResponse> RefreshAccessTokenAsync(string refreshToken, string clientId, string clientSecret);

    /// <summary>
    /// Получает broadcaster ID по имени канала
    /// </summary>
    /// <param name="channelName">Имя канала</param>
    /// <returns>Broadcaster ID или empty если пользователь не найден</returns>
    Task<string> GetBroadcasterIdAsync(string channelName);

    /// <summary>
    /// Проверяет валидность токена и обновляет его при необходимости.
    /// </summary>
    /// <param name="refreshToken">Refresh token для обновления</param>
    /// <param name="clientId">ClientId</param>
    /// <param name="clientSecret">ClientSecret</param>
    /// <returns>RefreshResponse с новыми токенами если было обновление, null если токен валиден</returns>
    Task<RefreshResponse?> EnsureValidTokenAsync(string refreshToken, string clientId, string clientSecret);

    /// <summary>
    /// Возвращает количество зрителей канала, или null если стрим оффлайн.
    /// </summary>
    Task<int?> GetViewerCountAsync(string channel);

    /// <summary>
    /// Возвращает словарь rewardId → title для всех кастомных наград канала.
    /// Требует токен с scope channel:read:redemptions.
    /// </summary>
    Task<Dictionary<string, string>> GetCustomRewardsAsync(string broadcasterId, string broadcasterAccessToken);

    /// <summary>
    /// Обменивает authorization code на access/refresh токены пользователя.
    /// </summary>
    Task<AuthCodeResponse> ExchangeCodeForTokensAsync(string code, string clientId, string clientSecret, string redirectUri);

    /// <summary>
    /// Возвращает информацию о токене (userId, login, scopes) или null если токен невалиден.
    /// </summary>
    Task<ValidateAccessTokenResponse?> GetTokenInfoAsync(string accessToken);

    /// <summary>
    /// Обновляет токен по refresh-токену, НЕ меняя сохранённый в TwitchAPI токен приложения.
    /// Используется для токенов авторизованного пользователя.
    /// </summary>
    Task<RefreshResponse> RefreshTokenAsync(string refreshToken, string clientId, string clientSecret);
}