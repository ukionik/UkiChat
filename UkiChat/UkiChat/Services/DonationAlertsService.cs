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

    // Возрастающая задержка переподключения: стартовая, множитель и потолок.
    private static readonly TimeSpan InitialReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(30);

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
            StartupDiagnostics.Log("da", $"Disconnected: reason={e.Reason} reconnect={e.Reconnect}");
            await SendNotification(_localizationService.GetString("donationalerts.disconnected"));
            if (!_intentionalDisconnect && e.Reconnect)
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
            await ConnectWithTokenAsync(settings.AccessToken);
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("da", $"Connection error: {ex.Message}", ex);
            await SendNotification(_localizationService.GetString("donationalerts.connectingError"));
        }
    }

    public Task DisconnectAsync() => _client.DisconnectAsync();

    /// <summary>
    ///     Получает данные пользователя (валидируя/обновляя токен) и подключает Centrifuge-клиент.
    /// </summary>
    private async Task ConnectWithTokenAsync(string accessToken)
    {
        var (validToken, user) = await GetUserWithValidTokenAsync(accessToken);
        var channel = $"$alerts:donation_{user.Data.Id}";

        await _client.ConnectAsync(user.Data.SocketConnectionToken, channel,
            clientId => _apiService.GetCentrifugeSubscribeTokenAsync(validToken, clientId, channel));
    }

    /// <summary>
    ///     Запрашивает /user/oauth; при 401 обновляет токен по refresh_token и повторяет.
    ///     Возвращает действующий access-токен и данные пользователя (один сетевой вызов в норме).
    /// </summary>
    private async Task<(string AccessToken, Model.DonationAlerts.DonationAlertsUserResponse User)> GetUserWithValidTokenAsync(string accessToken)
    {
        try
        {
            var user = await _apiService.GetUserAsync(accessToken);
            return (accessToken, user);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("DonationAlerts access token истёк, обновляем");
            var refreshed = await RefreshTokensAsync();
            var user = await _apiService.GetUserAsync(refreshed);
            return (refreshed, user);
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
            if (_reconnectCts is { IsCancellationRequested: false })
                return;

            _reconnectCts?.Dispose();
            var cts = new CancellationTokenSource();
            _reconnectCts = cts;
            _ = Task.Run(() => ReconnectLoopAsync(cts));
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

    private async Task ReconnectLoopAsync(CancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;
        var delay = InitialReconnectDelay;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(delay, cancellationToken);
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

                    await ConnectWithTokenAsync(settings.AccessToken);
                    _logger.LogInformation("DonationAlerts переподключение успешно");
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("DonationAlerts ошибка переподключения (следующая попытка через {Delay} с): {Message}",
                        delay.TotalSeconds, ex.Message);
                    // Удваиваем задержку до потолка MaxReconnectDelay.
                    delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxReconnectDelay.Ticks));
                }
            }
        }
        finally
        {
            // Освобождаем _reconnectCts при выходе из цикла, чтобы следующий разрыв смог
            // запустить новый цикл (иначе живой неотменённый CTS навсегда блокирует StartReconnectLoop).
            lock (_reconnectLock)
            {
                if (_reconnectCts == cts)
                {
                    _reconnectCts.Dispose();
                    _reconnectCts = null;
                }
            }
        }
    }

    private async Task SendNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromDonationAlertsNotification(message));
    }
}
