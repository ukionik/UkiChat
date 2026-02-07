using System;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;

namespace UkiChat.Services;

public class TwitchApiService : ITwitchApiService
{
    private TwitchAPI? _api;
    private bool _isInitialized;

    public Task InitializeAsync(string clientId, string accessToken)
    {
        _api = new TwitchAPI
        {
            Settings =
            {
                ClientId = clientId,
                AccessToken = accessToken
            }
        };
        _isInitialized = true;
        return Task.CompletedTask;
    }

    public async Task<bool> ValidateAccessTokenAsync()
    {
        EnsureInitialized();
        var result = await _api!.Auth.ValidateAccessTokenAsync();
        return result != null;
    }

    public async Task<GetGlobalChatBadgesResponse> GetGlobalChatBadgesAsync()
    {
        EnsureInitialized();
        return await _api!.Helix.Chat.GetGlobalChatBadgesAsync();
    }

    public async Task<GetChannelChatBadgesResponse> GetChannelChatBadgesAsync(string broadcasterId)
    {
        EnsureInitialized();
        return await _api!.Helix.Chat.GetChannelChatBadgesAsync(broadcasterId);
    }

    public async Task<RefreshResponse> RefreshAccessTokenAsync(string refreshToken, string clientId, string clientSecret)
    {
        EnsureInitialized();
        var response = await _api!.Auth.RefreshAuthTokenAsync(refreshToken, clientSecret, clientId);

        // Обновляем AccessToken в текущем экземпляре API
        _api.Settings.AccessToken = response.AccessToken;

        return response;
    }

    public async Task<string> GetBroadcasterIdAsync(string channelName)
    {
        EnsureInitialized();

        var users = await _api!.Helix.Users.GetUsersAsync(logins: [channelName.ToLower()]);
        return users.Users.Length > 0 ? users.Users[0].Id : "";
    }

    public async Task<RefreshResponse?> EnsureValidTokenAsync(string refreshToken, string clientId, string clientSecret)
    {
        EnsureInitialized();

        var validationResult = await _api!.Auth.ValidateAccessTokenAsync();

        // Токен валиден - обновление не требуется
        if (validationResult != null)
            return null;

        // Токен невалиден - обновляем
        return await RefreshAccessTokenAsync(refreshToken, clientId, clientSecret);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("TwitchApiService not initialized. Call InitializeAsync first.");
    }
}
