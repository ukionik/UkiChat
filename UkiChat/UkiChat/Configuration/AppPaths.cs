using System;
using System.IO;

namespace UkiChat.Configuration;

/// <summary>
///     Единая точка разрешения путей приложения.
///
///     Каталог с exe (<see cref="BaseDirectory" />) — это то, куда распакован архив версии, и он
///     у каждой версии свой. Пользовательские данные там жить не должны: обновление сводилось к
///     ручному переносу файлов, а перенести только ukichat.db без ukichat.key нельзя — база
///     зашифрована, и новая установка молча заводила чистую (см. DatabaseContext.OpenOrRecreate).
///     Поэтому данные лежат в <see cref="DataDirectory" /> — общем для всех версий
///     %APPDATA%\UkiChat: распаковал новую версию, запустил, всё на месте.
///
///     Портативный режим (флешка, «ничего не писать в систему») включается файлом-маркером
///     <see cref="PortableMarkerFileName" /> рядом с exe — тогда данные снова считаются от
///     каталога с exe, как было раньше.
///
///     В отладочной сборке данные тоже лежат рядом с exe (в bin), чтобы отладка не работала
///     с базой установленного приложения — там живые токены и каналы.
///
///     Пути к статике по-прежнему считаются от каталога с exe: wwwroot — часть сборки, а не данные.
/// </summary>
internal static class AppPaths
{
    /// <summary>Файл-маркер рядом с exe, переводящий приложение в портативный режим</summary>
    public const string PortableMarkerFileName = "portable.txt";

    private const string DataFolderName = "UkiChat";

    /// <summary>Каталог, в котором лежит exe</summary>
    public static string BaseDirectory { get; } = AppContext.BaseDirectory;

    /// <summary>Включён ли портативный режим (рядом с exe лежит portable.txt)</summary>
    public static bool IsPortable { get; } = File.Exists(Path.Combine(BaseDirectory, PortableMarkerFileName));

    // Порядок объявления важен: статические инициализаторы выполняются сверху вниз,
    // и DataDirectory ниже опирается на уже вычисленный результат.
    private static readonly (string Path, string Reason) ResolvedData = ResolveDataDirectory();

    /// <summary>
    ///     Каталог пользовательских данных: база, ключ шифрования, логи.
    ///     %APPDATA%\UkiChat, либо каталог с exe — в портативном режиме и под отладкой.
    /// </summary>
    public static string DataDirectory { get; } = EnsureDirectory(ResolvedData.Path);

    /// <summary>Почему каталог данных именно такой. Только для стартовой диагностики.</summary>
    public static string DataDirectoryReason => ResolvedData.Reason;

    /// <summary>
    ///     Данные лежат рядом с exe, а не в общем каталоге. Тогда переносить нечего:
    ///     каталог данных и есть каталог установки.
    /// </summary>
    public static bool DataIsNextToExe => PathEquals(DataDirectory, BaseDirectory);

    /// <summary>Каталог со статикой Nuxt, раздаваемой Kestrel</summary>
    public static string WwwRoot { get; } = Path.Combine(BaseDirectory, "wwwroot");

    /// <summary>Каталог логов. Создаётся при первом обращении.</summary>
    public static string LogsDirectory { get; } = EnsureDirectory(Path.Combine(DataDirectory, "logs"));

    /// <summary>Путь к файлу лога внутри <see cref="LogsDirectory" /></summary>
    public static string LogFile(string fileName)
    {
        return Path.Combine(LogsDirectory, fileName);
    }

    /// <summary>
    ///     Разрешает путь из app-settings: абсолютный оставляем как есть (пользователь может
    ///     сознательно вынести БД на другой диск), относительный считаем от каталога данных.
    /// </summary>
    public static string ResolveDataPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Путь не может быть пустым", nameof(path));

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(DataDirectory, path));
    }

    private static (string Path, string Reason) ResolveDataDirectory()
    {
        if (IsPortable)
            return (BaseDirectory, "portable");

#if DEBUG
        // Отладка НЕ должна трогать базу установленного приложения: там живые токены и каналы,
        // а под отладкой база легко портится экспериментом или недописанной миграцией.
        // Держим данные в bin, как было до переезда в %APPDATA%: снёс bin — получил чистый старт.
        // Проверка через #if, а не через файл-маркер в выводе сборки: маркер, случайно попавший
        // в релизный publish, перевёл бы в портативный режим ВСЕХ пользователей.
        return (BaseDirectory, "debug");
#else
        // %APPDATA% может быть недоступен в экзотических конфигурациях (пустая переменная
        // окружения у сервисных учёток). Падать из-за этого незачем — откатываемся к каталогу
        // с exe, то есть к прежнему поведению.
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrWhiteSpace(roaming)
            ? (BaseDirectory, "appdata-unavailable")
            : (Path.Combine(roaming, DataFolderName), "appdata");
#endif
    }

    private static bool PathEquals(string a, string b)
    {
        return string.Equals(
            a.TrimEnd(Path.DirectorySeparatorChar),
            b.TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch
        {
            // Каталог логов не должен ронять старт приложения: если создать не удалось,
            // Serilog и StartupDiagnostics молча не запишут файл.
        }

        return path;
    }
}
