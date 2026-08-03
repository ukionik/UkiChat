using System;
using System.Collections.Generic;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using UkiChat.Data.DefaultAppSettingsData;
using UkiChat.Diagnostics;
using UkiChat.Entities;
using UkiChat.Model.DonationAlerts;
using UkiChat.Model.VkVideoLive;
using UkiChat.Model.YouTube;
using UkiChat.Services;

namespace UkiChat.Configuration;

public static class DIConfiguration
{
    // Все созданные Serilog-логгеры: пишут асинхронно, поэтому при выходе их нужно закрыть,
    // иначе последние записи (в том числе о самом завершении) остаются в буфере.
    private static readonly List<Logger> Loggers = [];

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

        // Строго до первого обращения к базе и к ключу: переносит данные из установки, которая
        // держала их рядом с exe. Обратный порядок завёл бы чистую базу поверх ненайденной старой.
        LegacyDataMigrator.MigrateIfNeeded();

        var databasePath = AppPaths.ResolveDataPath(appSettings.Database.Filename);
        StartupDiagnostics.Log("database", $"Файл БД: {databasePath}");

        // Пароль не из app-settings: тот файл вшит в сборку и читается кем угодно.
        var connectionString = new ConnectionString
        {
            Filename = databasePath,
            Password = DatabaseKeyProvider.GetOrCreateKey()
        };

        services.AddSingleton<IDatabaseContext>(_ => new DatabaseContext(connectionString, appSettings));

        ConfigureLogging(services);

        return services;
    }

    /// <summary>
    ///     Сбрасывает на диск и закрывает все логгеры. Вызывается при выходе — процесс завершается
    ///     принудительно (см. App.OnExit), и асинхронные синки иначе теряют хвост записей.
    /// </summary>
    public static void CloseLoggers()
    {
        foreach (var logger in Loggers)
        {
            try
            {
                logger.Dispose();
            }
            catch
            {
                // Выход не должен падать из-за логов
            }
        }

        Loggers.Clear();
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        var logger = CreateLogger(LogEventLevel.Information, "log-.txt", RollingInterval.Day);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger, dispose: true);
        });

        var sessionTimestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        var vkChatLogger = CreateLogger(LogEventLevel.Debug, $"vk-video-live-chat-{sessionTimestamp}.txt");

        services.AddSingleton(
            LoggerFactory.Create(b => b.AddSerilog(vkChatLogger, dispose: true))
                .CreateLogger<VkVideoLiveChatClient>());

        var youTubeChatLogger = CreateLogger(LogEventLevel.Debug, $"youtube-chat-{sessionTimestamp}.txt");

        services.AddSingleton(
            LoggerFactory.Create(b => b.AddSerilog(youTubeChatLogger, dispose: true))
                .CreateLogger<YouTubeChatClient>());

        var twitchChatLogger = CreateLogger(LogEventLevel.Debug, $"twitch-chat-log-{sessionTimestamp}.txt");

        services.AddSingleton(
            LoggerFactory.Create(b => b.AddSerilog(twitchChatLogger, dispose: true))
                .CreateLogger<TwitchChatService>());

        var donationAlertsLogger = CreateLogger(LogEventLevel.Debug, $"donation-alerts-{sessionTimestamp}.txt");

        services.AddSingleton(
            LoggerFactory.Create(b => b.AddSerilog(donationAlertsLogger, dispose: true))
                .CreateLogger<DonationAlertsCentrifugeClient>());
    }

    private static Logger CreateLogger(LogEventLevel level, string fileName,
        RollingInterval rollingInterval = RollingInterval.Infinite)
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .WriteTo.Async(a => a.File(AppPaths.LogFile(fileName), rollingInterval: rollingInterval))
            .CreateLogger();

        Loggers.Add(logger);
        return logger;
    }
}