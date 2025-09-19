using Microsoft.Extensions.DependencyInjection;
using AppSettingsReader = UkiChat.Data.AppSettingsData.AppSettingsReader;

namespace UkiChat.Configuration;

public static class DIConfiguration
{
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        
        services.Scan(scan => scan
            .FromAssemblyOf<App>()
            .AddClasses(classes => classes.InNamespaces("UkiChat.Services"))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        var appSettings = AppSettingsReader.Read();
        services.AddSingleton<IDatabaseContext>(_ =>
            new DatabaseContext($@"Filename={appSettings.Database.Filename};Password={appSettings.Database.Password}", appSettings.Twitch)
        );

        return services;
    }
}