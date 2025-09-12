using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UkiChat.ViewModels;
using UkiChat.Web;

namespace UkiChat;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private HttpServer _server;

    public App()
    {
        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.Scan(scan => scan
                    .FromAssemblyOf<App>()
                    .AddClasses(classes => classes.InNamespaces("UkiChat.Services"))
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime());

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            }).Build();
        InitHttpServer();
    }

    private static IHost? AppHost { get; set; }

    private void InitHttpServer()
    {
        _server = new HttpServer();
        _server.Start();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();
        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        base.OnExit(e);
    }
}