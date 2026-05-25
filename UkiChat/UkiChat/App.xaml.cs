using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using ControlzEx.Theming;
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
    private readonly IWebHost _webHost;
    private IAppInitializationService? _appInitializationService;

    public App()
    {
        StartupDiagnostics.Log("app", "App.ctor: BEGIN");
        InstallGlobalExceptionHandlers();
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

            using (StartupDiagnostics.Measure("app", "_webHost.StartAsync()"))
            {
                await _webHost.StartAsync();
            }
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
        }
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

    protected override async void OnExit(ExitEventArgs e)
    {
        StartupDiagnostics.Log("app", "OnExit: BEGIN");
        try
        {
            await _webHost.StopAsync();
            _webHost.Dispose();
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("app", "OnExit FAILED", ex);
        }
        base.OnExit(e);
        StartupDiagnostics.Log("app", "OnExit: END");
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
