using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UkiChat.Configuration;

namespace UkiChat;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private IHttpServer? HttpServer => AppHost?.Services.GetRequiredService<IHttpServer>();

    public App()
    {
        AppHost = DIConfiguration.CreateAppHost();
    }

    private static IHost? AppHost { get; set; }
    protected override async void OnStartup(StartupEventArgs e)
    {
        await AppHost!.StartAsync();
        await HttpServer!.StartAsync()!;
        var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        await HttpServer!.StopAsync();
        base.OnExit(e);
    }
}