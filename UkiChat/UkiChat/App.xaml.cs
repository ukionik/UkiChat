using System;
using System.Windows;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Prism.Container.DryIoc;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using UkiChat.Configuration;
using UkiChat.Hubs;
using UkiChat.Services;

namespace UkiChat;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private readonly IWebHost _webHost = HttpServerConfiguration.CreateHost();

    protected override async void OnInitialized()
    {
        base.OnInitialized();
        await _webHost.StartAsync();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
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
        var services = DIConfiguration.CreateServices();
        services.AddSingleton<IWebHost>(_ => _webHost);
        var hubContext = _webHost.Services.GetRequiredService<IHubContext<AppHub>>();
        
        // Интеграция MS.DI с DryIoc
        var container = containerRegistry.GetContainer();
        // Запуск SignalR клиента
        container.Populate(services);
        container.RegisterInstance(hubContext);
        
        var localizationService = container.Resolve<ILocalizationService>();
        localizationService.SetCulture("ru");
    }

    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _webHost.StopAsync();
        _webHost.Dispose();
        base.OnExit(e);
    }
}