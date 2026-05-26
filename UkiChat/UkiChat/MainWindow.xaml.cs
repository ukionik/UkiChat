using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using UkiChat.Configuration;
using UkiChat.Diagnostics;
using UkiChat.ViewModels;

namespace UkiChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            StartupDiagnostics.Log("mainwin", "MainWindow.ctor: BEGIN");
            InitializeComponent();
            StartupDiagnostics.Log("mainwin", "MainWindow.ctor: InitializeComponent done");

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
            ContentRendered += (_, _) => StartupDiagnostics.Log("mainwin", "ContentRendered");
            Activated += (_, _) => StartupDiagnostics.Log("mainwin", "Activated");
            SourceInitialized += (_, _) => StartupDiagnostics.Log("mainwin", "SourceInitialized");

            HookWebViewEvents();
            StartupDiagnostics.Log("mainwin", "MainWindow.ctor: END");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartupDiagnostics.Log("mainwin", "MainWindow Loaded");
            _ = EnsureWebView2Async();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            StartupDiagnostics.Log("mainwin", "MainWindow Closed");
        }

        private async System.Threading.Tasks.Task EnsureWebView2Async()
        {
            try
            {
                StartupDiagnostics.Log("webview2", "EnsureCoreWebView2Async: BEGIN");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var env = await WebView2EnvironmentHelper.GetEnvironmentAsync();
                await WebView.EnsureCoreWebView2Async(env);
                sw.Stop();
                StartupDiagnostics.Log("webview2", $"EnsureCoreWebView2Async: END took={sw.ElapsedMilliseconds} ms");

                StartupDiagnostics.Log("webview2",
                    $"Runtime version: {env.BrowserVersionString}; UserDataFolder: {env.UserDataFolder}");

                // Source ставим вручную после Ensure, чтобы XAML-биндинг не успел
                // инициализировать WebView2 с дефолтным Environment
                if (DataContext is MainWindowViewModel vm && !string.IsNullOrEmpty(vm.WebSource))
                {
                    WebView.Source = new Uri(vm.WebSource);
                }
            }
            catch (Exception ex)
            {
                StartupDiagnostics.LogError("webview2", "EnsureCoreWebView2Async FAILED", ex);
            }
        }

        private void HookWebViewEvents()
        {
            WebView.CoreWebView2InitializationCompleted += (s, e) =>
            {
                if (e.IsSuccess)
                {
                    StartupDiagnostics.Log("webview2", "CoreWebView2InitializationCompleted: SUCCESS");
                    HookCoreWebView2Events();
                }
                else
                {
                    StartupDiagnostics.LogError("webview2",
                        "CoreWebView2InitializationCompleted: FAILED", e.InitializationException);
                }
            };

            WebView.NavigationStarting += (s, e) =>
                StartupDiagnostics.Log("webview2", $"NavigationStarting: {e.Uri}");

            WebView.NavigationCompleted += (s, e) =>
                StartupDiagnostics.Log("webview2",
                    $"NavigationCompleted: success={e.IsSuccess} status={e.WebErrorStatus} httpStatus={e.HttpStatusCode}");

            WebView.ContentLoading += (s, e) =>
                StartupDiagnostics.Log("webview2", $"ContentLoading: isError={e.IsErrorPage} id={e.NavigationId}");

            WebView.SourceChanged += (s, e) =>
                StartupDiagnostics.Log("webview2", $"SourceChanged: {WebView.Source}");
        }

        private void HookCoreWebView2Events()
        {
            var core = WebView.CoreWebView2;

            core.ProcessFailed += (s, e) =>
                StartupDiagnostics.LogError("webview2",
                    $"ProcessFailed: kind={e.ProcessFailedKind} reason={e.Reason} exitCode={e.ExitCode}");

            core.WebMessageReceived += (s, e) =>
            {
                try
                {
                    // Фронт форвардит console.log через postMessage в виде строки JSON
                    var message = e.TryGetWebMessageAsString();
                    StartupDiagnostics.Log("webview2-msg", message);
                }
                catch (Exception ex)
                {
                    StartupDiagnostics.LogError("webview2-msg", "failed to read WebMessageReceived", ex);
                }
            };

            core.DOMContentLoaded += (s, e) =>
                StartupDiagnostics.Log("webview2", $"DOMContentLoaded: navId={e.NavigationId}");

            core.HistoryChanged += (s, e) =>
                StartupDiagnostics.Log("webview2", $"HistoryChanged: source={core.Source}");

            // Ловим ошибки выполнения JS
            core.ScriptDialogOpening += (s, e) =>
                StartupDiagnostics.Log("webview2", $"ScriptDialogOpening: {e.Kind} {e.Message}");
        }
    }
}
