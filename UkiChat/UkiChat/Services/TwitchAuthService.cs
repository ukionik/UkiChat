using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UkiChat.Configuration;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public class TwitchAuthService : ITwitchAuthService
{
    // Должен быть точно зарегистрирован в OAuth Redirect URLs приложения на dev.twitch.tv.
    public const string RedirectUri = "http://localhost:5000/twitch/auth/callback";

    // Scope для чтения названий кастомных наград (channel points).
    private const string Scopes = "channel:read:redemptions";

    private const string AuthorizeEndpoint = "https://id.twitch.tv/oauth2/authorize";

    private readonly IDatabaseContext _databaseContext;
    private readonly IDatabaseService _databaseService;
    private readonly ITwitchApiService _twitchApiService;
    private readonly ISignalRService _signalRService;
    private readonly ITwitchChatService _twitchChatService;
    private readonly ILogger<TwitchAuthService> _logger;

    // CSRF-state текущей попытки авторизации.
    private string? _pendingState;

    public TwitchAuthService(
        IDatabaseContext databaseContext,
        IDatabaseService databaseService,
        ITwitchApiService twitchApiService,
        ISignalRService signalRService,
        ITwitchChatService twitchChatService,
        ILogger<TwitchAuthService> logger)
    {
        _databaseContext = databaseContext;
        _databaseService = databaseService;
        _twitchApiService = twitchApiService;
        _signalRService = signalRService;
        _twitchChatService = twitchChatService;
        _logger = logger;
    }

    public string BuildAuthorizeUrl()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(twitchSettings.ApiClientId))
        {
            _logger.LogWarning("BuildAuthorizeUrl: ApiClientId не настроен");
            return "";
        }

        _pendingState = Guid.NewGuid().ToString("N");

        return $"{AuthorizeEndpoint}" +
               $"?response_type=code" +
               $"&client_id={Uri.EscapeDataString(twitchSettings.ApiClientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
               $"&scope={Uri.EscapeDataString(Scopes)}" +
               $"&state={_pendingState}";
    }

    public async Task<bool> HandleCallbackAsync(string code, string state)
    {
        if (string.IsNullOrEmpty(_pendingState) || state != _pendingState)
        {
            _logger.LogWarning("HandleCallbackAsync: несовпадение state (CSRF?)");
            return false;
        }
        _pendingState = null;

        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(twitchSettings.ApiClientId) || string.IsNullOrEmpty(twitchSettings.ApiClientSecret))
        {
            _logger.LogWarning("HandleCallbackAsync: не настроены ApiClientId/ApiClientSecret");
            return false;
        }

        try
        {
            var tokens = await _twitchApiService.ExchangeCodeForTokensAsync(
                code, twitchSettings.ApiClientId, twitchSettings.ApiClientSecret, RedirectUri);

            var tokenInfo = await _twitchApiService.GetTokenInfoAsync(tokens.AccessToken);
            if (tokenInfo == null)
            {
                _logger.LogWarning("HandleCallbackAsync: не удалось валидировать полученный токен");
                return false;
            }

            _databaseService.UpdateTwitchUserTokens(
                tokens.AccessToken, tokens.RefreshToken, tokenInfo.UserId, tokenInfo.Login);
            _logger.LogInformation("Twitch авторизация успешна: {Login} ({UserId})",
                tokenInfo.Login, tokenInfo.UserId);

            await _twitchChatService.ReloadCustomRewardsAsync();
            await _signalRService.SendTwitchAuthChanged(GetStatus());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleCallbackAsync: ошибка обмена кода на токены");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        _databaseService.ClearTwitchUserAuth();
        _logger.LogInformation("Twitch авторизация сброшена");
        await _signalRService.SendTwitchAuthChanged(GetStatus());
    }

    public TwitchAuthStatusData GetStatus() => _databaseService.GetTwitchAuthStatus();
}
