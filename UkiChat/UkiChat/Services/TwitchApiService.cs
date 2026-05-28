using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;
using UkiChat.Diagnostics;

namespace UkiChat.Services;

public class TwitchApiService : ITwitchApiService
{
    private TwitchAPI? _api;
    private bool _isInitialized;

    public Task InitializeAsync(string clientId, string accessToken)
    {
        StartupDiagnostics.Log("twitch-api", "InitializeAsync");
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

    public Task<bool> ValidateAccessTokenAsync()
    {
        return MeasureAsync("ValidateAccessTokenAsync", async () =>
        {
            EnsureInitialized();
            var result = await _api!.Auth.ValidateAccessTokenAsync();
            return result != null;
        });
    }

    public Task<GetGlobalChatBadgesResponse> GetGlobalChatBadgesAsync()
    {
        return MeasureAsync("GetGlobalChatBadgesAsync", () =>
        {
            EnsureInitialized();
            return _api!.Helix.Chat.GetGlobalChatBadgesAsync();
        });
    }

    public Task<GetChannelChatBadgesResponse> GetChannelChatBadgesAsync(string broadcasterId)
    {
        return MeasureAsync($"GetChannelChatBadgesAsync({broadcasterId})", () =>
        {
            EnsureInitialized();
            return _api!.Helix.Chat.GetChannelChatBadgesAsync(broadcasterId);
        });
    }

    public Task<RefreshResponse> RefreshAccessTokenAsync(string refreshToken, string clientId, string clientSecret)
    {
        return MeasureAsync("RefreshAccessTokenAsync", async () =>
        {
            EnsureInitialized();
            var response = await _api!.Auth.RefreshAuthTokenAsync(refreshToken, clientSecret, clientId);
            _api.Settings.AccessToken = response.AccessToken;
            return response;
        });
    }

    public Task<string> GetBroadcasterIdAsync(string channelName)
    {
        return MeasureAsync($"GetBroadcasterIdAsync({channelName})", async () =>
        {
            EnsureInitialized();
            var users = await _api!.Helix.Users.GetUsersAsync(logins: [channelName.ToLower()]);
            return users.Users.Length > 0 ? users.Users[0].Id : "";
        });
    }

    public Task<RefreshResponse?> EnsureValidTokenAsync(string refreshToken, string clientId, string clientSecret)
    {
        return MeasureAsync("EnsureValidTokenAsync", async () =>
        {
            EnsureInitialized();
            var validationResult = await _api!.Auth.ValidateAccessTokenAsync();
            if (validationResult != null)
            {
                StartupDiagnostics.Log("twitch-api", "  token still valid");
                return (RefreshResponse?)null;
            }
            StartupDiagnostics.Log("twitch-api", "  token invalid, refreshing...");
            return await RefreshAccessTokenAsync(refreshToken, clientId, clientSecret);
        });
    }

    public Task<int?> GetViewerCountAsync(string channel)
    {
        return MeasureAsync($"GetViewerCountAsync({channel})", async () =>
        {
            EnsureInitialized();
            var response = await _api!.Helix.Streams.GetStreamsAsync(userLogins: [channel]);
            return response.Streams.Length > 0 ? (int?)response.Streams[0].ViewerCount : null;
        });
    }

    public Task<Dictionary<string, string>> GetCustomRewardsAsync(string broadcasterId, string broadcasterAccessToken)
    {
        return MeasureAsync($"GetCustomRewardsAsync({broadcasterId})", async () =>
        {
            EnsureInitialized();
            var response = await _api!.Helix.ChannelPoints.GetCustomRewardAsync(
                broadcasterId, accessToken: broadcasterAccessToken);
            return response.Data.ToDictionary(r => r.Id, r => r.Title);
        });
    }

    public Task<AuthCodeResponse> ExchangeCodeForTokensAsync(string code, string clientId, string clientSecret, string redirectUri)
    {
        return MeasureAsync("ExchangeCodeForTokensAsync", () =>
        {
            EnsureInitialized();
            return _api!.Auth.GetAccessTokenFromCodeAsync(code, clientSecret, redirectUri, clientId);
        });
    }

    public Task<ValidateAccessTokenResponse?> GetTokenInfoAsync(string accessToken)
    {
        return MeasureAsync("GetTokenInfoAsync", async () =>
        {
            EnsureInitialized();
            return (ValidateAccessTokenResponse?)await _api!.Auth.ValidateAccessTokenAsync(accessToken);
        });
    }

    public Task<RefreshResponse> RefreshTokenAsync(string refreshToken, string clientId, string clientSecret)
    {
        return MeasureAsync("RefreshTokenAsync", () =>
        {
            EnsureInitialized();
            // В отличие от RefreshAccessTokenAsync — НЕ перезаписываем _api.Settings.AccessToken,
            // чтобы токен приложения (для бейджей/зрителей) не подменялся пользовательским.
            return _api!.Auth.RefreshAuthTokenAsync(refreshToken, clientSecret, clientId);
        });
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("TwitchApiService not initialized. Call InitializeAsync first.");
    }

    private static async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        var sw = Stopwatch.StartNew();
        StartupDiagnostics.Log("twitch-api", $"-> {operation}");
        try
        {
            var result = await action();
            sw.Stop();
            StartupDiagnostics.Log("twitch-api", $"<- {operation} took={sw.ElapsedMilliseconds} ms");
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            StartupDiagnostics.LogError("twitch-api", $"!! {operation} took={sw.ElapsedMilliseconds} ms", ex);
            throw;
        }
    }
}
