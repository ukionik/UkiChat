using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using UkiChat.Data.DefaultAppSettingsData;
using UkiChat.Entities;

namespace UkiChat.Configuration;

public static class DIConfiguration
{
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        
        services.Scan(scan => scan
            .FromAssemblyOf<App>()
            .AddClasses(classes => classes.InNamespaces("UkiChat.Services", "UkiChat.Repositories.Memory"))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        services.AddSingleton(DefaultAppSettingsReader.Read());

        var appSettings = services.BuildServiceProvider().GetRequiredService<DefaultAppSettings>();

        services.AddSingleton<IDatabaseContext>(_ =>
            new DatabaseContext($@"Filename={appSettings.Database.Filename};Password={appSettings.Database.Password}", appSettings)
        );

        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Async(a => a.File("logs/log-.txt", rollingInterval: RollingInterval.Day))
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger, dispose: true);
        });

        return services;
    }
}