using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UkiChat.Diagnostics;

namespace UkiChat.Configuration;

/// <summary>
///     Ключ шифрования LiteDB. Раньше пароль лежал в app-settings.local.toml, который
///     вшивается в сборку как EmbeddedResource — то есть один и тот же пароль ехал всем
///     пользователям открытым текстом и доставался из DLL без декомпилятора. Шифрование
///     базы при этом не защищало ничего, хотя в базе лежат OAuth-токены.
///
///     Теперь ключ генерируется случайно при первом запуске и хранится рядом с exe,
///     зашифрованный DPAPI на учётную запись Windows: другой пользователь той же машины
///     файл прочитать не сможет.
///
///     Плата за это — ключ не переживает переноса папки на другой ПК или в другую учётку.
///     Такой случай не считается ошибкой: база пересоздаётся с нуля (см. DatabaseContext.OpenOrRecreate),
///     пользователь заново проходит OAuth.
/// </summary>
internal static class DatabaseKeyProvider
{
    private const string KeyFileName = "ukichat.key";

    /// <summary>
    ///     Возвращает пароль БД, создавая ключ при первом запуске.
    ///     Формат — hex, чтобы не думать о спецсимволах в строке подключения LiteDB.
    /// </summary>
    public static string GetOrCreateKey()
    {
        var keyPath = AppPaths.ResolveDataPath(KeyFileName);

        if (File.Exists(keyPath))
        {
            var existing = TryReadKey(keyPath);
            if (existing != null)
                return existing;

            // Файл есть, но расшифровать нечем: папку перенесли на другую машину/учётку
            // либо файл побился. Старый ключ невосстановим, поэтому генерируем новый —
            // база под него будет пересоздана в OpenOrRecreate.
            StartupDiagnostics.Log("database-key",
                "Файл ключа не расшифровывается — генерируется новый ключ");
        }

        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        WriteKey(keyPath, key);
        StartupDiagnostics.Log("database-key", $"Создан новый ключ БД: {keyPath}");
        return key;
    }

    private static string? TryReadKey(string keyPath)
    {
        try
        {
            var blob = File.ReadAllBytes(keyPath);
            var plain = ProtectedData.Unprotect(blob, null, DataProtectionScope.CurrentUser);
            var key = Encoding.UTF8.GetString(plain);
            return string.IsNullOrWhiteSpace(key) ? null : key;
        }
        catch (Exception ex)
        {
            StartupDiagnostics.LogError("database-key", "Не удалось прочитать файл ключа", ex);
            return null;
        }
    }

    private static void WriteKey(string keyPath, string key)
    {
        var blob = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyPath, blob);
    }
}
