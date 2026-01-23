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
    /// <returns>Broadcaster ID или null если пользователь не найден</returns>
    Task<string?> GetBroadcasterIdAsync(string channelName);

    /// <summary>
    /// Проверяет валидность токена и обновляет его при необходимости.
    /// </summary>
    /// <param name="refreshToken">Refresh token для обновления</param>
    /// <param name="clientId">ClientId</param>
    /// <param name="clientSecret">ClientSecret</param>
    /// <returns>RefreshResponse с новыми токенами если было обновление, null если токен валиден</returns>
    Task<RefreshResponse?> EnsureValidTokenAsync(string refreshToken, string clientId, string clientSecret);
}