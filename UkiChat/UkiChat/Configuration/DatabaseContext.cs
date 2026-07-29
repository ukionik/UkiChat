using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LiteDB;
using UkiChat.Data.DefaultAppSettingsData;
using UkiChat.Diagnostics;
using UkiChat.Entities;
using UkiChat.Repositories.Database;

namespace UkiChat.Configuration;

public class DatabaseContext : IDatabaseContext, IDisposable
{
    public DatabaseContext(ConnectionString connectionString
        , DefaultAppSettings defaultAppSettings)
    {
        var db = OpenOrRecreate(connectionString);
        ProfileRepository = new ProfileRepository(db);
        AppSettingsRepository = new AppSettingsRepository(db);
        TwitchSettingsRepository = new TwitchSettingsRepository(db);
        VkVideoLiveSettingsRepository = new VkVideoLiveSettingsRepository(db);
        YouTubeSettingsRepository = new YouTubeSettingsRepository(db);
        DonationAlertsSettingsRepository = new DonationAlertsSettingsRepository(db);
        SevenTvEmoteRepository = new SevenTvEmoteRepository(db);
        FfzEmoteRepository = new FfzEmoteRepository(db);
        BttvEmoteRepository = new BttvEmoteRepository(db);
        TwitchBadgeRepository = new TwitchBadgeRepository(db);
        InitDefaultData(defaultAppSettings);
    }

    public ITwitchSettingsRepository TwitchSettingsRepository { get; }
    public IVkVideoLiveSettingsRepository VkVideoLiveSettingsRepository { get; }
    public IYouTubeSettingsRepository YouTubeSettingsRepository { get; }
    public IDonationAlertsSettingsRepository DonationAlertsSettingsRepository { get; }
    public IProfileRepository ProfileRepository { get; }
    public IAppSettingsRepository AppSettingsRepository { get; }
    public ISevenTvEmoteRepository SevenTvEmoteRepository { get; }
    public IFfzEmoteRepository FfzEmoteRepository { get; }
    public IBttvEmoteRepository BttvEmoteRepository { get; }
    public ITwitchBadgeRepository TwitchBadgeRepository { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Открывает базу, а если текущий ключ к ней не подходит — отводит файл в сторону
    ///     и создаёт чистую. Такое возможно, когда портативную папку перенесли на другой ПК
    ///     или в другую учётку Windows: ключ защищён DPAPI и не переезжает вместе с файлами
    ///     (см. <see cref="DatabaseKeyProvider" />). Расшифровать старую базу в этом случае
    ///     нечем, поэтому единственный вариант — начать заново; файл сохраняем, чтобы
    ///     пользователь не потерял его молча.
    /// </summary>
    private static LiteDatabase OpenOrRecreate(ConnectionString connectionString)
    {
        try
        {
            return new LiteDatabase(connectionString);
        }
        catch (LiteException ex)
        {
            var suffix = $"unreadable-{DateTime.Now:yyyyMMdd-HHmmss}";
            var moved = 0;

            foreach (var file in DatabaseFiles(connectionString.Filename))
            {
                if (!File.Exists(file))
                    continue;

                File.Move(file, $"{file}.{suffix}");
                moved++;
            }

            // Отодвигать нечего — значит дело не в ключе, и глушить ошибку нельзя.
            if (moved == 0)
                throw;

            StartupDiagnostics.LogError("database",
                $"База не открывается текущим ключом, файлы отложены с суффиксом .{suffix} и создаются заново", ex);

            return new LiteDatabase(connectionString);
        }
    }

    /// <summary>
    ///     Сама база и её служебные файлы. LiteDB держит рядом с датафайлом "-log" и "-tmp",
    ///     зашифрованные тем же ключом: если отодвинуть только базу, новый файл подхватит
    ///     старый лог и снова упрётся в Invalid password.
    /// </summary>
    private static IEnumerable<string> DatabaseFiles(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);

        yield return path;
        yield return Path.Combine(directory, $"{name}-log{extension}");
        yield return Path.Combine(directory, $"{name}-tmp{extension}");
    }

    private void InitDefaultData(DefaultAppSettings defaultAppSettings)
    {
        InitDefaultProfile(defaultAppSettings);
        MigrateData(defaultAppSettings);
        MigrateYouTubeData();
    }

    // Миграция для БД, созданных до добавления YouTubeSettings
    private void MigrateYouTubeData()
    {
        if (YouTubeSettingsRepository.GetActiveSettings() != null)
            return;

        var appSettings = AppSettingsRepository.GetActiveAppSettings();
        if (appSettings == null)
            return;

        YouTubeSettingsRepository.Save(new YouTubeSettings { AppSettings = appSettings });
    }

    // Миграция для БД, созданных до добавления DonationAlertsSettings
    private void MigrateData(DefaultAppSettings defaultAppSettings)
    {
        if (DonationAlertsSettingsRepository.GetActiveSettings() != null)
            return;

        var appSettings = AppSettingsRepository.GetActiveAppSettings();
        if (appSettings == null)
            return;

        DonationAlertsSettingsRepository.Save(new DonationAlertsSettings
        {
            ClientId = defaultAppSettings.DonationAlerts.ClientId,
            ClientSecret = defaultAppSettings.DonationAlerts.ClientSecret,
            AppSettings = appSettings
        });
    }

    private void InitDefaultProfile(DefaultAppSettings defaultAppSettings)
    {
        if (ProfileRepository.Count() != 0)
            return;

        var profile = new Profile
        {
            Name = "Default",
            Active = true,
            Default = true
        };
        ProfileRepository.Save(profile);
        InitDefaultSettings(profile, defaultAppSettings);
    }

    private void InitDefaultSettings(Profile profile, DefaultAppSettings defaultAppSettings)
    {
        var appSettings = new AppSettings
        {
            Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            Profile = profile
        };
        AppSettingsRepository.Save(appSettings);
        // Токенов здесь намеренно нет: чат и Helix работают на токене авторизованного
        // пользователя, который появляется только после OAuth. До авторизации Twitch молчит.
        var twitchSettings = new TwitchSettings
        {
            ApiClientId = defaultAppSettings.Twitch.ApiClientId,
            ApiClientSecret = defaultAppSettings.Twitch.ApiClientSecret,
            AppSettings = appSettings,
        };
        TwitchSettingsRepository.Save(twitchSettings);

        var vkVideoLiveSettings = new VkVideoLiveSettings
        {
            ApiClientId = defaultAppSettings.VkVideoLive.ApiClientId,
            ApiClientSecret = defaultAppSettings.VkVideoLive.ApiClientSecret,
            AppSettings = appSettings
        };
        VkVideoLiveSettingsRepository.Save(vkVideoLiveSettings);

        var youTubeSettings = new YouTubeSettings
        {
            AppSettings = appSettings
        };
        YouTubeSettingsRepository.Save(youTubeSettings);

        var donationAlertsSettings = new DonationAlertsSettings
        {
            ClientId = defaultAppSettings.DonationAlerts.ClientId,
            ClientSecret = defaultAppSettings.DonationAlerts.ClientSecret,
            AppSettings = appSettings
        };
        DonationAlertsSettingsRepository.Save(donationAlertsSettings);
    }
}