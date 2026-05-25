using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace UkiChat.Diagnostics;

/// <summary>
///     Лог стартапа с таймингом от первого вызова. Пишет в logs/startup-{yyyyMMdd-HHmmss}.log
///     с явным flush на каждую запись — на случай если приложение упадёт до сброса буфера Serilog Async.
///     Дублирует в Console и (если доступен) в Serilog через ILogger.
/// </summary>
public static class StartupDiagnostics
{
    private static readonly Stopwatch _sw = Stopwatch.StartNew();
    private static readonly Lock _lock = new();
    private static readonly string _logFile;
    private static readonly DateTime _startedAt = DateTime.Now;

    static StartupDiagnostics()
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(dir);
        _logFile = Path.Combine(dir, $"startup-{_startedAt:yyyyMMdd-HHmmss}.log");

        // Сразу пишем шапку, чтобы файл создавался в момент первого обращения
        WriteRaw($"=== UkiChat startup diagnostics ===");
        WriteRaw($"Started at: {_startedAt:O}");
        WriteRaw($"CWD: {Directory.GetCurrentDirectory()}");
        WriteRaw($"OS: {Environment.OSVersion} ({(Environment.Is64BitOperatingSystem ? "x64" : "x86")})");
        WriteRaw($"CLR: {Environment.Version}");
        WriteRaw($"User: {Environment.UserDomainName}\\{Environment.UserName}");
        WriteRaw($"Machine: {Environment.MachineName}");
        WriteRaw($"ProcessorCount: {Environment.ProcessorCount}");
        WriteRaw($"WorkingSet: {Environment.WorkingSet / 1024 / 1024} MB");
        WriteRaw(string.Empty);
    }

    public static string LogFilePath => _logFile;

    public static void Log(string category, string message)
    {
        var elapsed = _sw.ElapsedMilliseconds;
        var line = $"[+{elapsed,7} ms] [{category,-22}] {message}";
        WriteRaw(line);
    }

    public static void LogError(string category, string message, Exception? ex = null)
    {
        var elapsed = _sw.ElapsedMilliseconds;
        var line = $"[+{elapsed,7} ms] [{category,-22}] ERROR: {message}";
        if (ex != null)
            line += Environment.NewLine + ex;
        WriteRaw(line);
    }

    /// <summary>Прозрачный таймер: возвращает IDisposable, который при Dispose пишет длительность</summary>
    public static IDisposable Measure(string category, string operation)
    {
        return new Timer(category, operation);
    }

    private static void WriteRaw(string line)
    {
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFile, line + Environment.NewLine);
            }
            catch
            {
                // диагностика не должна никогда падать сама
            }

            try
            {
                Console.WriteLine(line);
            }
            catch
            {
                // ignored
            }
        }
    }

    private sealed class Timer : IDisposable
    {
        private readonly string _category;
        private readonly string _operation;
        private readonly Stopwatch _opSw;

        public Timer(string category, string operation)
        {
            _category = category;
            _operation = operation;
            _opSw = Stopwatch.StartNew();
            Log(category, $"BEGIN {operation}");
        }

        public void Dispose()
        {
            _opSw.Stop();
            Log(_category, $"END   {_operation} (took {_opSw.ElapsedMilliseconds} ms)");
        }
    }
}
