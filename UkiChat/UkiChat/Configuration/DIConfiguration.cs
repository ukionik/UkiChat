using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UkiChat.ViewModels;
using AppSettingsReader = UkiChat.Data.AppSettingsData.AppSettingsReader;

namespace UkiChat.Configuration;

public static class DIConfiguration
{
    public static IHost CreateAppHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) => { Init(services); }).Build();
    }

    private static void Init(IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<App>()
            .AddClasses(classes => classes.InNamespaces("UkiChat.Services"))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        var appSettings = AppSettingsReader.Read();
        services.AddSingleton<IDatabaseContext>(_ =>
            new DatabaseContext($@"Filename={appSettings.Database.Filename};Password={appSettings.Database.Password}", appSettings.Twitch)
        );

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
    }
}