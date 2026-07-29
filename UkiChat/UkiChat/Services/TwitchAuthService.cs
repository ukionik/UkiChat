using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UkiChat.Configuration;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public class TwitchAuthService : ITwitchAuthService
{
    // Должен быть точно зарегистрирован в OAuth Redirect URLs приложения на dev.twitch.tv.
    public const string RedirectUri = "http://localhost:5000/twitch/auth/callback";

    // channel:read:redemptions — названия кастомных наград (channel points).
    // chat:read — подключение к IRC. Отдельного «токена чат-бота» больше нет: чат ездит на
    // токене авторизованного пользователя, поэтому scope запрашиваем сразу оба.
    // При изменении списка старые токены становятся недостаточными — см. HasRequiredScopes.
    public static readonly string[] RequiredScopes = ["channel:read:redemptions", "chat:read"];

    private static readonly string Scopes = string.Join(' ', RequiredScopes);

    private const string AuthorizeEndpoint = "https://id.twitch.tv/oauth2/authorize";

    /// <summary>
    ///     Хватает ли токену scope для текущей версии приложения. Токен, выданный до появления
    ///     chat:read, сам по себе валиден — Twitch его не отзовёт и refresh не доклеит недостающий
    ///     scope, — но чат на нём не поднимется. Такую авторизацию считаем негодной и просим
    ///     пройти заново.
    /// </summary>
    public static bool HasRequiredScopes(IEnumerable<string>? tokenScopes)
    {
        if (tokenScopes == null) return false;
        var granted = new HashSet<string>(tokenScopes, StringComparer.OrdinalIgnoreCase);
        return RequiredScopes.All(granted.Contains);
    }

    private readonly IDatabaseContext _databaseContext;
    private readonly IDatabaseService _databaseService;
    private readonly ITwitchApiService _twitchApiService;
    private readonly ISignalRService _signalRService;
    private readonly ITwitchChatService _twitchChatService;
    private readonly ITwitchEventSubService _twitchEventSubService;
    private readonly ILogger<TwitchAuthService> _logger;

    // CSRF-state текущей попытки авторизации.
    private string? _pendingState;

    public TwitchAuthService(
        IDatabaseContext databaseContext,
        IDatabaseService databaseService,
        ITwitchApiService twitchApiService,
        ISignalRService signalRService,
        ITwitchChatService twitchChatService,
        ITwitchEventSubService twitchEventSubService,
        ILogger<TwitchAuthService> logger)
    {
        _databaseContext = databaseContext;
        _databaseService = databaseService;
        _twitchApiService = twitchApiService;
        _signalRService = signalRService;
        _twitchChatService = twitchChatService;
        _twitchEventSubService = twitchEventSubService;
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

            if (!HasRequiredScopes(tokenInfo.Scopes))
                _logger.LogWarning("Выданный токен без части запрошенных scope: {Scopes} — чат не поднимется",
                    string.Join(' ', tokenInfo.Scopes ?? []));

            // На старте без авторизации API поднимался с пустым токеном (иначе нечем было бы
            // выполнить обмен кода выше). Теперь токен есть — пересоздаём клиент с ним,
            // иначе все Helix-вызовы отвечали бы 401 до перезапуска приложения.
            await _twitchApiService.InitializeAsync(twitchSettings.ApiClientId, tokens.AccessToken);

            // Чат ездит на этом же токене — переподключаем его с новыми учётными данными.
            await _twitchChatService.ReapplyCredentialsAsync();

            // Значки грузятся через Helix: на старте без авторизации загрузка не прошла,
            // и без повторной попытки чат остался бы без значков до перезапуска.
            await _twitchChatService.LoadGlobalDataAsync();
            await _twitchChatService.LoadChannelDataAsync();
            await _twitchChatService.ReloadCustomRewardsAsync();
            await _twitchEventSubService.RestartAsync();
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
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();

        _databaseService.ClearTwitchUserAuth();
        await _twitchEventSubService.StopAsync();
        // Токена больше нет — чат обязан отвалиться, иначе продолжит читать по отозванным
        // учётным данным до первого разрыва соединения.
        await _twitchChatService.ReapplyCredentialsAsync();

        // Сбрасываем токен и в Helix-клиенте, оставляя его инициализированным: следующая
        // авторизация снова пойдёт через ExchangeCodeForTokensAsync на этом же объекте.
        if (!string.IsNullOrEmpty(twitchSettings.ApiClientId))
            await _twitchApiService.InitializeAsync(twitchSettings.ApiClientId, "");
        _logger.LogInformation("Twitch авторизация сброшена");
        await _signalRService.SendTwitchAuthChanged(GetStatus());
    }

    public TwitchAuthStatusData GetStatus() => _databaseService.GetTwitchAuthStatus();
}
