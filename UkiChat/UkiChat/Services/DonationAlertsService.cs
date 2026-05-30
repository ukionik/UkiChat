using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UkiChat.Configuration;
using UkiChat.Diagnostics;
using UkiChat.Model.Chat;
using UkiChat.Model.DonationAlerts;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

/// <summary>
///     Оркестрация Donation Alerts: OAuth-авторизация + подключение к Centrifugo и приём донатов.
///     Подключение возможно только для авторизованного пользователя, поэтому auth и WebSocket
///     объединены в одном сервисе.
/// </summary>
public class DonationAlertsService : IDonationAlertsService
{
    // Должен быть зарегистрирован в Redirect URLs приложения на donationalerts.com.
    public const string RedirectUri = "http://localhost:5000/donationalerts/auth/callback";

    private const string AuthorizeEndpoint = "https://www.donationalerts.com/oauth/authorize";
    private const string Scopes = "oauth-user-show oauth-donation-subscribe";

    private readonly IDatabaseContext _databaseContext;
    private readonly IDatabaseService _databaseService;
    private readonly IDonationAlertsApiService _apiService;
    private readonly ISignalRService _signalRService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<DonationAlertsService> _logger;

    private readonly DonationAlertsCentrifugeClient _client;

    private readonly Lock _reconnectLock = new();
    private CancellationTokenSource? _reconnectCts;
    private volatile bool _intentionalDisconnect;

    // CSRF-state текущей попытки авторизации.
    private string? _pendingState;

    public DonationAlertsService(
        IDatabaseContext databaseContext,
        IDatabaseService databaseService,
        IDonationAlertsApiService apiService,
        ISignalRService signalRService,
        ILocalizationService localizationService,
        ILogger<DonationAlertsService> logger,
        ILogger<DonationAlertsCentrifugeClient> clientLogger)
    {
        _databaseContext = databaseContext;
        _databaseService = databaseService;
        _apiService = apiService;
        _signalRService = signalRService;
        _localizationService = localizationService;
        _logger = logger;
        _client = new DonationAlertsCentrifugeClient(clientLogger);

        _client.DonationReceived += async (_, e) =>
        {
            if (e.Donation == null) return;
            var donorName = string.IsNullOrEmpty(e.Donation.Username) ? "Anonymous" : e.Donation.Username;
            await _signalRService.SendChatMessageAsync(UkiChatMessage.FromDonationAlertsDonation(
                donorName, e.Donation.Amount, e.Donation.Currency ?? "", e.Donation.Message ?? ""));
        };

        _client.Connected += async (_, _) =>
        {
            StartupDiagnostics.Log("da", "Connected");
            await SendNotification(_localizationService.GetString("donationalerts.connected"));
        };

        _client.Disconnected += async (_, e) =>
        {
            StartupDiagnostics.Log("da", $"Disconnected: reason={e.Reason}");
            await SendNotification(_localizationService.GetString("donationalerts.disconnected"));
            if (!_intentionalDisconnect)
                StartReconnectLoop();
        };

        _client.Error += (_, e) => StartupDiagnostics.LogError("da", $"Error: {e.Message}");
    }

    public string BuildAuthorizeUrl()
    {
        var settings = _databaseContext.DonationAlertsSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(settings.ClientId))
        {
            _logger.LogWarning("BuildAuthorizeUrl: ClientId не настроен");
            return "";
        }

        _pendingState = Guid.NewGuid().ToString("N");

        return $"{AuthorizeEndpoint}" +
               $"?client_id={Uri.EscapeDataString(settings.ClientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
               $"&response_type=code" +
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

        var settings = _databaseContext.DonationAlertsSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(settings.ClientId) || string.IsNullOrEmpty(settings.ClientSecret))
        {
            _logger.LogWarning("HandleCallbackAsync: не настроены ClientId/ClientSecret");
            return false;
        }

        try
        {
            var tokens = await _apiService.ExchangeCodeForTokensAsync(
                code, settings.ClientId, settings.ClientSecret, RedirectUri);

            var user = await _apiService.GetUserAsync(tokens.AccessToken);

            _databaseService.UpdateDonationAlertsUserTokens(
                tokens.AccessToken, tokens.RefreshToken, user.Data.Id.ToString(), user.Data.Name);
            _logger.LogInformation("DonationAlerts авторизация успешна: {Name} ({Id})",
                user.Data.Name, user.Data.Id);

            _intentionalDisconnect = false;
            await ConnectAsync();
            await _signalRService.SendDonationAlertsAuthChanged(GetStatus());
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
        _intentionalDisconnect = true;
        CancelReconnectLoop();
        await DisconnectAsync();
        _databaseService.ClearDonationAlertsUserAuth();
        _logger.LogInformation("DonationAlerts авторизация сброшена");
        await _signalRService.SendDonationAlertsAuthChanged(GetStatus());
    }

    public DonationAlertsAuthStatusData GetStatus() => _databaseService.GetDonationAlertsAuthStatus();

    public async Task ConnectAsync()
    {
        using var _ = StartupDiagnostics.Measure("da", "ConnectAsync");
        var settings = _databaseContext.DonationAlertsSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(settings.AccessToken))
        {
            StartupDiagnostics.Log("da", "Не авторизован, подключение пропущено");
            return;
        }

        CancelReconnectLoop();
        _intentionalDisconnect = false;

        try
        {
            await SendNotification(_localizationService.GetString("donationalerts.connecting"));

            var accessToken = await EnsureValidAccessTokenAsync(settings.AccessToken);
            var user = await _apiService.GetUserAsync(accessToken);
            var channel = $"$alerts:donation_{user.Data.Id}";

            await _client.ConnectAsync(user.Data.SocketConnectionToken, channel,
                clientId => _apiService.GetCentrifugeSubscribeTokenAsync(accessToken, clientId, channel));
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("da", $"Connection error: {ex.Message}", ex);
            await SendNotification(_localizationService.GetString("donationalerts.connectingError"));
        }
    }

    public Task DisconnectAsync() => _client.DisconnectAsync();

    /// <summary>
    ///     Проверяет access-токен через /user/oauth; при 401 обновляет его по refresh_token.
    ///     Возвращает действующий access-токен.
    /// </summary>
    private async Task<string> EnsureValidAccessTokenAsync(string accessToken)
    {
        try
        {
            await _apiService.GetUserAsync(accessToken);
            return accessToken;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("DonationAlerts access token истёк, обновляем");
            return await RefreshTokensAsync();
        }
    }

    private async Task<string> RefreshTokensAsync()
    {
        var settings = _databaseContext.DonationAlertsSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(settings.RefreshToken) ||
            string.IsNullOrEmpty(settings.ClientId) || string.IsNullOrEmpty(settings.ClientSecret))
            throw new InvalidOperationException("Нет данных для обновления токена DonationAlerts");

        var tokens = await _apiService.RefreshTokensAsync(settings.RefreshToken, settings.ClientId, settings.ClientSecret);
        _databaseService.UpdateDonationAlertsUserTokens(
            tokens.AccessToken, tokens.RefreshToken, settings.UserId ?? "", settings.UserName ?? "");
        return tokens.AccessToken;
    }

    private void StartReconnectLoop()
    {
        lock (_reconnectLock)
        {
            if (_reconnectCts != null && !_reconnectCts.IsCancellationRequested)
                return;

            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _reconnectCts = new CancellationTokenSource();
            _ = Task.Run(() => ReconnectLoopAsync(_reconnectCts.Token));
        }
    }

    private void CancelReconnectLoop()
    {
        lock (_reconnectLock)
        {
            _reconnectCts?.Cancel();
            _reconnectCts?.Dispose();
            _reconnectCts = null;
        }
    }

    private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                var settings = _databaseContext.DonationAlertsSettingsRepository.GetActiveSettings();
                if (string.IsNullOrEmpty(settings.AccessToken))
                    return;

                var accessToken = await EnsureValidAccessTokenAsync(settings.AccessToken);
                var user = await _apiService.GetUserAsync(accessToken);
                var channel = $"$alerts:donation_{user.Data.Id}";

                await _client.ConnectAsync(user.Data.SocketConnectionToken, channel,
                    clientId => _apiService.GetCentrifugeSubscribeTokenAsync(accessToken, clientId, channel));
                _logger.LogInformation("DonationAlerts переподключение успешно");
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("DonationAlerts ошибка переподключения: {Message}", ex.Message);
            }
        }
    }

    private async Task SendNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromDonationAlertsNotification(message));
    }
}
