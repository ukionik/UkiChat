using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UkiChat.Diagnostics;

namespace UkiChat.Configuration;

/// <summary>
///     Разовый перенос данных из установки, которая ещё держала их рядом с exe (версии по 0.7.1
///     включительно). Начиная с 0.7.2 данные живут в <see cref="AppPaths.DataDirectory" />, общем
///     для всех версий, и переносить больше нечего.
///
///     Копируем именно файлы базы, а не настройки по одной: ключ шифрования защищён DPAPI на
///     учётную запись Windows, а перенос идёт в пределах той же учётки и той же машины — то есть
///     ключ читается, и вместе с настройками переезжает авторизация. Заново логиниться не нужно.
///
///     Старые файлы НЕ удаляем: если перенос окажется неудачным, у пользователя остаётся оригинал.
/// </summary>
internal static class LegacyDataMigrator
{
    private const string DatabaseFileName = "ukichat.db";
    private const string KeyFileName = "ukichat.key";

    // LiteDB держит рядом с датафайлом служебные "-log" и "-tmp", зашифрованные тем же ключом.
    // Оставить "-log" позади нельзя: в нём могут лежать последние записи, не слитые в основной
    // файл, а подхваченный от чужой базы лог упирается в Invalid password (см. DatabaseContext).
    private static readonly string[] FilesToCopy =
    [
        DatabaseFileName,
        "ukichat-log.db",
        "ukichat-tmp.db",
        KeyFileName
    ];

    public static void MigrateIfNeeded()
    {
        try
        {
            Migrate();
        }
        catch (Exception ex)
        {
            // Не переносится — не беда: приложение просто заведёт чистую базу, как и раньше.
            // Ронять из-за этого старт нельзя.
            StartupDiagnostics.LogError("data-migration", "Перенос данных из старой установки не удался", ex);
        }
    }

    private static void Migrate()
    {
        // Портативный режим и отладка держат данные рядом с exe — каталог данных и есть каталог
        // установки, переносить нечего. Заодно это защищает от копирования файла в самого себя:
        // каталог с exe — первый кандидат в поиске предыдущей установки.
        if (AppPaths.DataIsNextToExe)
            return;

        var target = AppPaths.DataDirectory;
        if (File.Exists(Path.Combine(target, DatabaseFileName)))
            return;

        var source = FindPreviousInstall(target);
        if (source == null)
        {
            StartupDiagnostics.Log("data-migration", "Предыдущая установка не найдена — начинаем с чистой базы");
            return;
        }

        var copied = 0;
        foreach (var fileName in FilesToCopy)
        {
            var from = Path.Combine(source, fileName);
            if (!File.Exists(from))
                continue;

            File.Copy(from, Path.Combine(target, fileName), overwrite: true);
            copied++;
        }

        StartupDiagnostics.Log("data-migration",
            $"Данные перенесены из {source} ({copied} файлов); оригиналы оставлены на месте");
    }

    /// <summary>
    ///     Ищет каталог предыдущей установки: тот, где рядом лежат и база, и ключ от неё.
    ///     Без ключа каталог бесполезен — расшифровать базу будет нечем.
    /// </summary>
    private static string? FindPreviousInstall(string target)
    {
        return Candidates(target)
            .Where(dir => File.Exists(Path.Combine(dir, DatabaseFileName))
                          && File.Exists(Path.Combine(dir, KeyFileName)))
            // Установок может оказаться несколько — берём ту, где базу писали последней.
            .OrderByDescending(dir => File.GetLastWriteTimeUtc(Path.Combine(dir, DatabaseFileName)))
            .FirstOrDefault();
    }

    private static IEnumerable<string> Candidates(string target)
    {
        var baseDir = AppPaths.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

        // 1. Каталог с exe. Срабатывает, когда новую версию распаковали ПОВЕРХ старой папки —
        //    самый частый способ обновиться.
        yield return baseDir;

        var parent = Path.GetDirectoryName(baseDir);
        if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
            yield break;

        // 2. Соседние каталоги распакованных версий (UkiChat-0.7.1-win-x64 рядом с UkiChat-0.7.2-win-x64)
        //    и один уровень внутри них: архив часто распаковывают "в папку с именем архива",
        //    и получается UkiChat-0.7.1-win-x64\UkiChat-0.7.1-win-x64.
        //    Ограничиваемся именами с "ukichat", чтобы не перебирать весь Рабочий стол.
        foreach (var sibling in SafeEnumerateDirectories(parent))
        {
            if (PathEquals(sibling, baseDir) || PathEquals(sibling, target))
                continue;

            if (!Path.GetFileName(sibling).Contains("ukichat", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return sibling;

            foreach (var nested in SafeEnumerateDirectories(sibling))
            {
                if (!PathEquals(nested, baseDir))
                    yield return nested;
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string path)
    {
        try
        {
            return Directory.EnumerateDirectories(path);
        }
        catch
        {
            // Нет прав на каталог — просто пропускаем его
            return [];
        }
    }

    private static bool PathEquals(string a, string b)
    {
        return string.Equals(
            a.TrimEnd(Path.DirectorySeparatorChar),
            b.TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
