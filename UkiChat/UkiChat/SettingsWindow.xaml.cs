using System;
using System.Windows;
using UkiChat.Configuration;
using UkiChat.ViewModels;

namespace UkiChat;

public partial class SettingsWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += SettingsWindow_Loaded;
    }

    private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var env = await WebView2EnvironmentHelper.GetEnvironmentAsync();
            await WebView.EnsureCoreWebView2Async(env);

            if (DataContext is SettingsWindowViewModel vm && !string.IsNullOrEmpty(vm.WebSource))
            {
                WebView.Source = new Uri(vm.WebSource);
            }
        }
        catch
        {
            // Fail-safe: при ошибке Environment упадём на дефолт, чтобы окно всё-таки открылось
            if (DataContext is SettingsWindowViewModel vm && !string.IsNullOrEmpty(vm.WebSource))
            {
                await WebView.EnsureCoreWebView2Async();
                WebView.Source = new Uri(vm.WebSource);
            }
        }
    }
}
