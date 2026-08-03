using System.Reflection;

namespace UkiChat.Configuration;

/// <summary>
///     Сведения о самой сборке. Нужны в первую очередь диагностике: логи приходят от пользователей
///     без указания версии, а поведение приложения от версии к версии меняется — по логу без
///     версии непонятно, воспроизводится ли уже исправленная проблема.
/// </summary>
internal static class AppInfo
{
    /// <summary>Версия в виде "0.7.2" — как её видит пользователь в строке состояния</summary>
    public static string Version { get; } = BuildVersion();

    /// <summary>
    ///     Версия с конфигурацией сборки: "0.7.2 (Release)". Для логов, чтобы отладочный запуск
    ///     не путали с тем, что скачал пользователь.
    /// </summary>
    public static string VersionWithConfiguration { get; } =
#if DEBUG
        $"{Version} (Debug)";
#else
        $"{Version} (Release)";
#endif

    private static string BuildVersion()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        return v == null
            ? "unknown"
            : $"{v.Major}.{v.Minor}.{v.Build}";
    }
}
