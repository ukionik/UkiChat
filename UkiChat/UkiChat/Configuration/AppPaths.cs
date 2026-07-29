using System;
using System.IO;

namespace UkiChat.Configuration;

/// <summary>
///     Единая точка разрешения путей приложения.
///     Всё считается от каталога с exe (<see cref="AppContext.BaseDirectory" />), а не от текущего
///     рабочего каталога: CWD зависит от способа запуска (ярлык с чужим "Рабочей папкой",
///     запуск из консоли, автозагрузка), и при относительных путях БД с логами уезжали
///     в произвольное место, а wwwroot переставал раздаваться.
///     Портативность при этом сохраняется — данные лежат рядом с exe.
/// </summary>
internal static class AppPaths
{
    /// <summary>Каталог, в котором лежит exe</summary>
    public static string BaseDirectory { get; } = AppContext.BaseDirectory;

    /// <summary>Каталог со статикой Nuxt, раздаваемой Kestrel</summary>
    public static string WwwRoot { get; } = Path.Combine(BaseDirectory, "wwwroot");

    /// <summary>Каталог логов. Создаётся при первом обращении.</summary>
    public static string LogsDirectory { get; } = EnsureDirectory(Path.Combine(BaseDirectory, "logs"));

    /// <summary>Путь к файлу лога внутри <see cref="LogsDirectory" /></summary>
    public static string LogFile(string fileName)
    {
        return Path.Combine(LogsDirectory, fileName);
    }

    /// <summary>
    ///     Разрешает путь из app-settings: абсолютный оставляем как есть (пользователь может
    ///     сознательно вынести БД на другой диск), относительный считаем от каталога с exe.
    /// </summary>
    public static string ResolveDataPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Путь не может быть пустым", nameof(path));

        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(BaseDirectory, path));
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
