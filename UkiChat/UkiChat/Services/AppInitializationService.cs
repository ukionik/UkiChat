using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UkiChat.Configuration;
using UkiChat.Diagnostics;
using UkiChat.Entities;
using UkiChat.Model.Twitch;
using UkiChat.Model.VkVideoLive;
using UkiChat.Model.YouTube;

namespace UkiChat.Services;

public class AppInitializationService(
    ILocalizationService localizationService,
    IFrontendReadyService frontendReadyService,
    IDatabaseContext databaseContext,
    IDatabaseService databaseService,
    ITwitchChatService twitchChatService,
    ITwitchApiService twitchApiService,
    ITwitchEventSubService twitchEventSubService,
    IVkVideoLiveChatService vkVideoLiveChatService,
    IVkVideoLiveApiService vkVideoLiveApiService,
    IYouTubeChatService youTubeChatService,
    IDonationAlertsService donationAlertsService
) : IAppInitializationService
{
    // Сколько ждём фронтенд, прежде чем записать в лог, что он так и не подключился.
    private static readonly TimeSpan FrontendReadyLogTimeout = TimeSpan.FromSeconds(30);

    // Повторы инициализации Twitch API: типичная причина осечки — приложение стартовало раньше,
    // чем поднялась сеть после включения ПК, и через несколько секунд всё уже работает.
    private const int TwitchApiRetryMaxAttempts = 10;
    private static readonly TimeSpan TwitchApiInitialRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan TwitchApiMaxRetryDelay = TimeSpan.FromSeconds(60);

    public async Task InitializeAsync()
    {
        StartupDiagnostics.Log("app-init", "InitializeAsync: BEGIN");
        using (StartupDiagnostics.Measure("app-init", "SetCulture(ru)"))
        {
            localizationService.SetCulture("ru");
        }

        // Готовность фронтенда БОЛЬШЕ НЕ БЛОКИРУЕТ подъём чатов. Раньше здесь стояло ожидание с
        // таймаутом 30с, и если фронт не подключался (например, WebView2 получил 404 на wwwroot
        // без index.html), TimeoutException рвал инициализацию целиком: Twitch, VK, YouTube и
        // DonationAlerts не стартовали вовсе, а исключение молча гасилось в App.OnInitialized.
        // Чатам фронт не нужен — подключившись, он сам забирает у хаба всё, что ему требуется.
        // Ждём его только ради записи в лог.
        _ = LogFrontendReadinessAsync();

        using (StartupDiagnostics.Measure("app-init", "LoadTwitchData + LoadVkVideoLiveData (parallel)"))
        {
            await Task.WhenAll(
                RunIsolatedAsync("Twitch", LoadTwitchDataAsync),
                RunIsolatedAsync("VkVideoLive", LoadVkVideoLiveDataAsync),
                RunIsolatedAsync("YouTube", LoadYouTubeDataAsync),
                RunIsolatedAsync("DonationAlerts", LoadDonationAlertsDataAsync));
        }
        StartupDiagnostics.Log("app-init", "InitializeAsync: END");
    }

    // Диагностика: видно, дождались мы фронтенд или нет, но на подъём чатов это уже не влияет.
    private async Task LogFrontendReadinessAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await frontendReadyService.WaitAsync().WaitAsync(FrontendReadyLogTimeout);
            StartupDiagnostics.Log("app-init", $"Фронтенд подключился за {sw.ElapsedMilliseconds} мс");
        }
        catch (TimeoutException)
        {
            StartupDiagnostics.Log("app-init",
                $"Фронтенд не подключился за {FrontendReadyLogTimeout.TotalSeconds:F0}с (на работу чатов не влияет)");
        }
    }

    // Каждая площадка поднимается независимо: сломанные настройки или недоступный API одной
    // не должны мешать остальным — раньше исключение из Task.WhenAll обрывало InitializeAsync.
    private static async Task RunIsolatedAsync(string name, Func<Task> load)
    {
        try
        {
            await load();
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app-init", $"Инициализация {name} не удалась", ex);
        }
    }

    private async Task LoadTwitchDataAsync()
    {
        using var _ = StartupDiagnostics.Measure("app-init", "LoadTwitchDataAsync");
        TwitchSettings twitchSettings;
        using (StartupDiagnostics.Measure("app-init", "  read TwitchSettings from DB"))
        {
            twitchSettings = databaseContext.TwitchSettingsRepository.GetActiveSettings();
        }
        StartupDiagnostics.Log("app-init",
            $"  Twitch channel={twitchSettings.Channel ?? "<none>"} hasApi={!string.IsNullOrEmpty(twitchSettings.ApiClientId)}");

        // Инициализация Helix API НЕ должна мешать чату: чат живёт на своём IRC-подключении и
        // работает без API (без значков). Раньше исключение отсюда (SocketException, когда DNS
        // ещё не поднялся после включения ПК) обрывало LoadTwitchDataAsync до ConnectAsync —
        // чат не подключался вовсе и не пытался снова до перезапуска.
        var apiReady = await TryInitializeTwitchApiAsync(twitchSettings);

        // Перечитываем настройки: внутри могли обновиться токены пользователя, а чат теперь
        // подключается именно ими — со старым объектом он ушёл бы в IRC с просроченным токеном.
        twitchSettings = databaseContext.TwitchSettingsRepository.GetActiveSettings();

        using (StartupDiagnostics.Measure("app-init", "  Twitch parallel: LoadGlobalData + LoadChannelData + ConnectAsync"))
        {
            await Task.WhenAll(
                twitchChatService.LoadGlobalDataAsync(),
                twitchChatService.LoadChannelDataAsync(),
                twitchChatService.ConnectAsync(
                    TwitchConnectionParams.OfTwitchSettings("", twitchSettings.Channel ?? "", twitchSettings))
            );
        }

        if (!apiReady)
        {
            // API догоняем в фоне; вместе с ним доедут значки, награды и EventSub.
            StartTwitchApiRetryLoop();
            return;
        }

        // Поднимаем EventSub для наград без текста (если пользователь авторизован)
        using (StartupDiagnostics.Measure("app-init", "  Twitch EventSub StartAsync"))
        {
            await twitchEventSubService.StartAsync();
        }
    }

    private async Task<bool> TryInitializeTwitchApiAsync(TwitchSettings twitchSettings)
    {
        using var _ = StartupDiagnostics.Measure("app-init", "  InitializeTwitchApi");
        try
        {
            await InitializeTwitchApiAsync(twitchSettings);
            return true;
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app-init", "[Twitch] Инициализация API не удалась", ex);
            return false;
        }
    }

    private void StartTwitchApiRetryLoop() => _ = TwitchApiRetryLoopAsync();

    /// <summary>
    ///     Догоняет инициализацию Twitch API в фоне с нарастающей задержкой. После успеха
    ///     перезагружает всё, чему API нужен: значки, награды канала и EventSub.
    /// </summary>
    private async Task TwitchApiRetryLoopAsync()
    {
        var delay = TwitchApiInitialRetryDelay;
        for (var attempt = 1; attempt <= TwitchApiRetryMaxAttempts; attempt++)
        {
            await Task.Delay(delay);

            // Настройки перечитываем: за время ожидания токены мог обновить кто-то ещё.
            var settings = databaseContext.TwitchSettingsRepository.GetActiveSettings();
            if (!await TryInitializeTwitchApiAsync(settings))
            {
                StartupDiagnostics.Log("app-init",
                    $"[Twitch] Повтор инициализации API: попытка {attempt}/{TwitchApiRetryMaxAttempts} не удалась");
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, TwitchApiMaxRetryDelay.Ticks));
                continue;
            }

            StartupDiagnostics.Log("app-init", $"[Twitch] API поднялся с попытки {attempt}, догружаем данные");
            try
            {
                await twitchChatService.LoadGlobalDataAsync();
                await twitchChatService.LoadChannelDataAsync();
                await twitchEventSubService.StartAsync();
            }
            catch (Exception ex)
            {
                StartupDiagnostics.LogError("app-init", "[Twitch] Догрузка данных после подъёма API не удалась", ex);
            }

            return;
        }

        StartupDiagnostics.LogError("app-init",
            $"[Twitch] API не поднялся после {TwitchApiRetryMaxAttempts} попыток");
    }

    private async Task InitializeTwitchApiAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.ApiClientId))
        {
            StartupDiagnostics.Log("app-init", "Twitch API client id is null or empty");
            return;
        }

        if (string.IsNullOrEmpty(twitchSettings.ApiClientSecret))
        {
            StartupDiagnostics.Log("app-init", "Twitch API client secret is null or empty");
            return;
        }

        // Отдельного токена приложения (client_credentials) больше нет: Helix ходит токеном
        // авторизованного пользователя — он принимается всеми вызовами, которые нам нужны
        // (бейджи, users, streams, награды, EventSub). Поэтому сначала приводим в порядок
        // пользовательский токен, и только потом инициализируем API уже свежим значением.
        await RefreshTwitchUserTokenAsync(twitchSettings);

        // Перечитываем: RefreshTwitchUserTokenAsync пишет новые токены в БД, а объект
        // twitchSettings остался с теми, что были прочитаны до обновления.
        var userAccessToken = databaseContext.TwitchSettingsRepository.GetActiveSettings().UserAccessToken;
        if (string.IsNullOrEmpty(userAccessToken))
            StartupDiagnostics.Log("app-init",
                "[Twitch] Нет авторизации пользователя — Helix-вызовы работать не будут, поднимаем API только под OAuth");

        // Инициализируем ВСЕГДА, даже с пустым токеном. Обмен кода на токены (OAuth-callback)
        // идёт через этот же TwitchApiService и падает на EnsureInitialized, если объект не
        // создан, — а именно этим вызовом первый токен и добывается. Получилась бы петля:
        // без токена нет API, без API не получить токен. Сам обмен access-токен не использует,
        // ему хватает client_id/client_secret, которые передаются аргументами.
        await twitchApiService.InitializeAsync(twitchSettings.ApiClientId, userAccessToken ?? "");
    }

    private async Task RefreshTwitchUserTokenAsync(TwitchSettings twitchSettings)
    {
        if (string.IsNullOrEmpty(twitchSettings.UserAccessToken) ||
            string.IsNullOrEmpty(twitchSettings.UserRefreshToken) ||
            string.IsNullOrEmpty(twitchSettings.ApiClientId) ||
            string.IsNullOrEmpty(twitchSettings.ApiClientSecret))
            return;

        // Токен ещё валиден — ничего не делаем
        var tokenInfo = await twitchApiService.GetTokenInfoAsync(twitchSettings.UserAccessToken);
        if (tokenInfo != null)
        {
            // Токен может быть валиден и при этом негоден: выдан до того, как приложению
            // понадобился chat:read. Twitch недостающий scope при refresh не доклеивает —
            // спасает только повторная авторизация, поэтому сбрасываем и просим войти заново.
            if (!TwitchAuthService.HasRequiredScopes(tokenInfo.Scopes))
            {
                StartupDiagnostics.Log("app-init",
                    "[Twitch] Сохранённому токену не хватает scope — требуется повторная авторизация");
                databaseService.ClearTwitchUserAuth();
                return;
            }

            StartupDiagnostics.Log("app-init", "[Twitch] User access token is valid");
            return;
        }

        try
        {
            var refreshed = await twitchApiService.RefreshTokenAsync(
                twitchSettings.UserRefreshToken, twitchSettings.ApiClientId, twitchSettings.ApiClientSecret);

            // Refresh возвращает тот же набор scope, что был выдан изначально — проверяем и его,
            // иначе негодный токен просто продлится и чат снова не поднимется.
            if (!TwitchAuthService.HasRequiredScopes(refreshed.Scopes))
            {
                StartupDiagnostics.Log("app-init",
                    "[Twitch] Обновлённому токену не хватает scope — требуется повторная авторизация");
                databaseService.ClearTwitchUserAuth();
                return;
            }

            databaseService.UpdateTwitchUserTokens(
                refreshed.AccessToken, refreshed.RefreshToken,
                twitchSettings.UserId ?? "", twitchSettings.UserLogin ?? "");
            StartupDiagnostics.Log("app-init", "[Twitch] User access token refreshed");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app-init", "[Twitch] User token refresh failed, clearing auth", ex);
            databaseService.ClearTwitchUserAuth();
        }
    }

    private async Task LoadVkVideoLiveDataAsync()
    {
        using var _ = StartupDiagnostics.Measure("app-init", "LoadVkVideoLiveDataAsync");
        var vkSettings = databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();
        StartupDiagnostics.Log("app-init",
            $"  VK channel={vkSettings.Channel ?? "<none>"} hasApi={!string.IsNullOrEmpty(vkSettings.ApiClientId)}");

        if (string.IsNullOrEmpty(vkSettings.ApiClientId) || string.IsNullOrEmpty(vkSettings.ApiClientSecret))
        {
            StartupDiagnostics.Log("app-init", "[VkVideoLive] API credentials not configured");
            return;
        }

        using (StartupDiagnostics.Measure("app-init", "  InitializeVkVideoLiveApi"))
        {
            await InitializeVkVideoLiveApiAsync(vkSettings);
        }

        using (StartupDiagnostics.Measure("app-init", "  VkVideoLive ConnectAsync"))
        {
            await vkVideoLiveChatService.ConnectAsync(
                VkVideoLiveConnectionParams.OfVkVideoLiveSettings("", vkSettings.Channel ?? "", vkSettings));
        }
    }

    private async Task LoadYouTubeDataAsync()
    {
        using var _ = StartupDiagnostics.Measure("app-init", "LoadYouTubeDataAsync");
        var youTubeSettings = databaseContext.YouTubeSettingsRepository.GetActiveSettings();
        StartupDiagnostics.Log("app-init", $"  YouTube channel={youTubeSettings.Channel ?? "<none>"}");

        await youTubeChatService.ConnectAsync(
            YouTubeConnectionParams.OfYouTubeSettings("", youTubeSettings.Channel ?? "", youTubeSettings));
    }

    private async Task LoadDonationAlertsDataAsync()
    {
        using var _ = StartupDiagnostics.Measure("app-init", "LoadDonationAlertsDataAsync");
        var settings = databaseContext.DonationAlertsSettingsRepository.GetActiveSettings();
        StartupDiagnostics.Log("app-init",
            $"  DonationAlerts authorized={!string.IsNullOrEmpty(settings.AccessToken)}");

        if (string.IsNullOrEmpty(settings.AccessToken))
            return;

        await donationAlertsService.ConnectAsync();
    }

    private async Task InitializeVkVideoLiveApiAsync(VkVideoLiveSettings vkSettings)
    {
        // Проверяем валидность токена и обновляем при необходимости
        await RefreshVkVideoLiveApiTokenAsync(vkSettings);
        var apiAccessToken = vkSettings.ApiAccessToken!;

        // Получаем WsAccessToken
        using (StartupDiagnostics.Measure("app-init", "    GetWebSocketTokenAsync"))
        {
            var wsTokenResponse = await vkVideoLiveApiService.GetWebSocketTokenAsync(apiAccessToken);
            var wsAccessToken = wsTokenResponse.Data.Token;
            StartupDiagnostics.Log("app-init", "[VkVideoLive] WebSocket token received");
            databaseService.UpdateVkVideoLiveTokens(apiAccessToken, wsAccessToken);
        }
    }

    private async Task RefreshVkVideoLiveApiTokenAsync(VkVideoLiveSettings vkSettings)
    {
        if (string.IsNullOrEmpty(vkSettings.ApiAccessToken))
        {
            await FetchVkVideoLiveApiTokenAsync(vkSettings);
            return;
        }

        try
        {
            using (StartupDiagnostics.Measure("app-init", "    ValidateAccessTokenAsync (VK)"))
            {
                var tokenInfo = await vkVideoLiveApiService.ValidateAccessTokenAsync(vkSettings.ApiAccessToken);
                var expiredAt = DateTimeOffset.FromUnixTimeSeconds(tokenInfo.Data.ExpiredAt);
                if (expiredAt <= DateTimeOffset.UtcNow)
                {
                    StartupDiagnostics.Log("app-init", "[VkVideoLive] API access token expired, fetching new one");
                    await FetchVkVideoLiveApiTokenAsync(vkSettings);
                }
                else
                {
                    StartupDiagnostics.Log("app-init", "[VkVideoLive] API access token is valid");
                }
            }
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app-init", "[VkVideoLive] API access token validation failed, fetching new one", ex);
            await FetchVkVideoLiveApiTokenAsync(vkSettings);
        }
    }

    private async Task FetchVkVideoLiveApiTokenAsync(VkVideoLiveSettings vkSettings)
    {
        using (StartupDiagnostics.Measure("app-init", "    GetAccessTokenAsync (VK)"))
        {
            var tokenResponse = await vkVideoLiveApiService.GetAccessTokenAsync(
                vkSettings.ApiClientId!, vkSettings.ApiClientSecret!);
            vkSettings.ApiAccessToken = tokenResponse.AccessToken;
            StartupDiagnostics.Log("app-init", "[VkVideoLive] API access token refreshed");
        }
    }
}
