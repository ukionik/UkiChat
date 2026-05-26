using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace UkiChat.Configuration;

/// <summary>
///     Общий CoreWebView2Environment для всех WebView2 в приложении.
///     --no-proxy-server отключает WPAD/PAC-резолвер в Chromium: иначе на машинах без
///     WPAD-сервера каждый первый запрос (даже к localhost) висит ~21 с до таймаута.
/// </summary>
internal static class WebView2EnvironmentHelper
{
    private static readonly object Lock = new();
    private static Task<CoreWebView2Environment>? _envTask;

    public static Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        lock (Lock)
        {
            return _envTask ??= CreateAsync();
        }
    }

    private static Task<CoreWebView2Environment> CreateAsync()
    {
        // UserDataFolder в %LOCALAPPDATA% — не зависит от прав на папку с exe
        // и переживает переустановку
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UkiChat", "WebView2");
        Directory.CreateDirectory(userDataFolder);

        var options = new CoreWebView2EnvironmentOptions("--no-proxy-server");
        return CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
    }
}
