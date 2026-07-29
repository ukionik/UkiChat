using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using ControlzEx.Theming;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Prism.Container.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using UkiChat.Configuration;
using UkiChat.Diagnostics;
using UkiChat.Hubs;
using UkiChat.Services;

namespace UkiChat;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    /// <summary>
    ///     Сколько ждём освобождения порта предыдущим экземпляром приложения.
    ///     Штатно тот уходит меньше чем за секунду, запас — на медленные машины.
    /// </summary>
    private static readonly TimeSpan PortWaitTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Сколько ждём остановку Kestrel при выходе, прежде чем убить процесс принудительно.</summary>
    private static readonly TimeSpan WebHostStopTimeout = TimeSpan.FromSeconds(3);

    private readonly TaskCompletionSource<bool> _serverReady =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly IWebHost _webHost;
    private IAppInitializationService? _appInitializationService;

    /// <summary>
    ///     Завершается, когда Kestrel занял порт (true) или окончательно не смог (false).
    ///     Окна ждут этого перед навигацией: WebView2, ушедший на localhost:5000 раньше времени,
    ///     получает ERR_CONNECTION_REFUSED и сам страницу не перезагружает.
    /// </summary>
    public Task<bool> ServerReady => _serverReady.Task;

    public App()
    {
        StartupDiagnostics.Log("app", "App.ctor: BEGIN");
        InstallGlobalExceptionHandlers();

        // Отключаем системный proxy auto-detect (WPAD) для всех HttpClient в процессе.
        // Без этого на машинах без WPAD-сервера первый исходящий запрос висит ~21 с
        // на таймауте WinHTTP — это блокирует и Twitch, и VK, и WebView2-навигацию.
        HttpClient.DefaultProxy = new WebProxy();

        using (StartupDiagnostics.Measure("app", "HttpServerConfiguration.CreateHost"))
        {
            _webHost = HttpServerConfiguration.CreateHost();
        }
        StartupDiagnostics.Log("app", "App.ctor: END");
    }

    protected override async void OnInitialized()
    {
        StartupDiagnostics.Log("app", "OnInitialized: BEGIN");
        try
        {
            base.OnInitialized();
            StartupDiagnostics.Log("app", "OnInitialized: base.OnInitialized() returned");

            if (!await StartWebHostAsync())
                return;

            StartupDiagnostics.Log("app", "OnInitialized: Kestrel started, addresses listed below");

            try
            {
                var addressFeature = _webHost.ServerFeatures.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
                if (addressFeature != null)
                {
                    foreach (var addr in addressFeature.Addresses)
                        StartupDiagnostics.Log("app", $"  Kestrel listening on: {addr}");
                }
            }
            catch (Exception ex)
            {
                StartupDiagnostics.LogError("app", "failed to enumerate Kestrel addresses", ex);
            }

            // TCP-зонды: проверяем доступность localhost через IPv4 и IPv6
            _ = Task.Run(() => TcpProbe.ProbeLoopbackAsync());

            using (StartupDiagnostics.Measure("app", "AppInitializationService.InitializeAsync()"))
            {
                await _appInitializationService!.InitializeAsync();
            }
            StartupDiagnostics.Log("app", "OnInitialized: END");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app", "OnInitialized FAILED", ex);
            // Окна не должны остаться в ожидании сервера, который уже не поднимется.
            _serverReady.TrySetResult(false);
        }
    }

    /// <summary>
    ///     Поднимает Kestrel, дав предыдущему экземпляру приложения время отпустить порт.
    ///     Раньше исключение отсюда просто писалось в лог: окно открывалось, WebView2 грузил
    ///     страницу с ЧУЖОГО, ещё живого экземпляра, а собственный бэкенд не работал вовсе —
    ///     со стороны это выглядело как "приложение не запускается".
    /// </summary>
    private async Task<bool> StartWebHostAsync()
    {
        using (StartupDiagnostics.Measure("app", "wait for free port"))
        {
            if (!await HttpServerConfiguration.WaitForFreePortAsync(PortWaitTimeout))
            {
                // Всё равно пробуем стартовать: точную причину лучше получить от Kestrel.
                StartupDiagnostics.Log("app", "Порт занят и не освободился — пробуем стартовать");
            }
        }

        try
        {
            using (StartupDiagnostics.Measure("app", "_webHost.StartAsync()"))
            {
                await _webHost.StartAsync();
            }

            _serverReady.TrySetResult(true);
            return true;
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app", "Kestrel не запустился", ex);
            _serverReady.TrySetResult(false);
            ShowStartupErrorAndShutdown(IsAddressInUse(ex) ? "app.portBusy" : "app.serverStartFailed");
            return false;
        }
    }

    private static bool IsAddressInUse(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is AddressInUseException)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Без бэкенда приложение бесполезно: показываем причину и закрываемся, а не оставляем
    ///     пустое окно, из которого непонятно, что произошло.
    /// </summary>
    private void ShowStartupErrorAndShutdown(string messageKey)
    {
        try
        {
            var localizationService = Container.Resolve<ILocalizationService>();
            // Сюда попадаем раньше AppInitializationService, который обычно грузит строки.
            localizationService.SetCulture("ru");

            MessageBox.Show(
                localizationService.GetString(messageKey),
                localizationService.GetString("app.startupErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app", "Не удалось показать сообщение об ошибке старта", ex);
        }

        Shutdown();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        StartupDiagnostics.Log("app", "RegisterTypes: BEGIN");
        try
        {
            var accentColor = Color.FromRgb(55, 45, 120);
            var customTheme = RuntimeThemeGenerator.Current.GenerateRuntimeTheme("Dark", accentColor);
            ThemeManager.Current.AddTheme(customTheme);
            ThemeManager.Current.ChangeTheme(this, customTheme);

            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType =>
            {
                var viewName = viewType.FullName;
                var viewAssemblyName = viewType.Assembly.FullName;
                // Меняем namespace с Views на ViewModels
                var viewModelName = viewName!.Replace("UkiChat", "UkiChat.ViewModels") + "ViewModel";
                return Type.GetType($"{viewModelName}, {viewAssemblyName}");
            });

            // Prism сервисы
            containerRegistry.RegisterSingleton<IWindowService, WindowService>();
            // EventAggregator
            containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();

            // MS.DI контейнер
            using (StartupDiagnostics.Measure("app", "DIConfiguration.CreateServices"))
            {
                var services = DIConfiguration.CreateServices();
                services.AddSingleton<IWebHost>(_ => _webHost);
                var hubContext = _webHost.Services.GetRequiredService<IHubContext<AppHub>>();

                // Интеграция MS.DI с DryIoc
                var container = containerRegistry.GetContainer();
                // Запуск SignalR клиента
                container.Populate(services);
                container.RegisterInstance(hubContext);

                _appInitializationService = container.Resolve<IAppInitializationService>();
            }
            StartupDiagnostics.Log("app", "RegisterTypes: END");
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app", "RegisterTypes FAILED", ex);
            throw;
        }
    }

    protected override Window CreateShell()
    {
        StartupDiagnostics.Log("app", "CreateShell: BEGIN");
        try
        {
            var window = Container.Resolve<MainWindow>();
            StartupDiagnostics.Log("app", "CreateShell: MainWindow resolved");
            return window;
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app", "CreateShell FAILED", ex);
            throw;
        }
    }

    /// <summary>
    ///     Выход должен быть быстрым и полным: пока процесс жив, он держит порт 5000 и файл базы,
    ///     а значит повторный запуск приложения не сработает. Раньше метод был async void — WPF
    ///     не ждал его вовсе, всё после первого await выполнялось "как получится", и остановка
    ///     Kestrel растягивалась на секунды. Теперь ждём синхронно и с потолком по времени.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        StartupDiagnostics.Log("app", "OnExit: BEGIN");

        StopWebHost();
        CloseDatabase();

        base.OnExit(e);
        StartupDiagnostics.Log("app", "OnExit: END");

        // Логи пишутся асинхронно — сбрасываем на диск до принудительного выхода.
        DIConfiguration.CloseLoggers();

        // Страховка от зависших фоновых потоков (websocket-циклы площадок, насосы SignalR):
        // без неё процесс мог пережить закрытие окна и не отдать порт следующему запуску.
        Environment.Exit(e.ApplicationExitCode);
    }

    private void StopWebHost()
    {
        try
        {
            // Task.Run, чтобы остановка не зависела от диспетчера UI, который уже завершается.
            if (!Task.Run(() => _webHost.StopAsync()).Wait(WebHostStopTimeout))
                StartupDiagnostics.Log("app",
                    $"OnExit: Kestrel не остановился за {WebHostStopTimeout.TotalSeconds:F0} с");

            _webHost.Dispose();
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app", "OnExit: остановка Kestrel не удалась", ex);
        }
    }

    private void CloseDatabase()
    {
        try
        {
            // LiteDB на Dispose сливает -log в основной файл; без этого он растёт от запуска к запуску.
            (Container.Resolve<IDatabaseContext>() as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app", "OnExit: закрытие БД не удалось", ex);
        }
    }

    private void InstallGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            StartupDiagnostics.LogError("unhandled", $"AppDomain.UnhandledException (IsTerminating={args.IsTerminating})", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            StartupDiagnostics.LogError("unhandled", "TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved();
        };

        DispatcherUnhandledException += (_, args) =>
        {
            StartupDiagnostics.LogError("unhandled", "Dispatcher.UnhandledException", args.Exception);
            // Не падаем — пытаемся продолжить, чтобы у пользователя был шанс собрать логи
            args.Handled = true;
        };
    }
}
