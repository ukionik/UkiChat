using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using UkiChat.Data.DefaultAppSettingsData;
using UkiChat.Entities;
using UkiChat.Model.DonationAlerts;
using UkiChat.Model.VkVideoLive;
using UkiChat.Model.YouTube;
using UkiChat.Services;

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

        ConfigureLogging(services);

        return services;
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Async(a => a.File("logs/log-.txt", rollingInterval: RollingInterval.Day))
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger, dispose: true);
        });

        var sessionTimestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        var vkChatLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Async(a => a.File($"logs/vk-video-live-chat-{sessionTimestamp}.txt"))
            .CreateLogger();

        services.AddSingleton(
            LoggerFactory.Create(b => b.AddSerilog(vkChatLogger, dispose: true))
                .CreateLogger<VkVideoLiveChatClient>());

        var youTubeChatLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Async(a => a.File($"logs/youtube-chat-{sessionTimestamp}.txt"))
            .CreateLogger();

        services.AddSingleton(
            LoggerFactory.Create(b => b.AddSerilog(youTubeChatLogger, dispose: true))
                .CreateLogger<YouTubeChatClient>());

        var twitchChatLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Async(a => a.File($"logs/twitch-chat-log-{sessionTimestamp}.txt"))
            .CreateLogger();

        services.AddSingleton(
            LoggerFactory.Create(b => b.AddSerilog(twitchChatLogger, dispose: true))
                .CreateLogger<TwitchChatService>());

        var donationAlertsLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Async(a => a.File($"logs/donation-alerts-{sessionTimestamp}.txt"))
            .CreateLogger();

        services.AddSingleton(
            LoggerFactory.Create(b => b.AddSerilog(donationAlertsLogger, dispose: true))
                .CreateLogger<DonationAlertsCentrifugeClient>());
    }
}