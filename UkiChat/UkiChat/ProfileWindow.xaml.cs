using System;
using System.Windows;
using UkiChat.Configuration;
using UkiChat.ViewModels;

namespace UkiChat;

public partial class ProfileWindow
{
    public ProfileWindow()
    {
        InitializeComponent();
        Loaded += ProfileWindow_Loaded;
    }

    private async void ProfileWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var env = await WebView2EnvironmentHelper.GetEnvironmentAsync();
            await WebView.EnsureCoreWebView2Async(env);

            if (DataContext is ProfileWindowViewModel vm && !string.IsNullOrEmpty(vm.WebSource))
            {
                WebView.Source = new Uri(vm.WebSource);
            }
        }
        catch
        {
            if (DataContext is ProfileWindowViewModel vm && !string.IsNullOrEmpty(vm.WebSource))
            {
                await WebView.EnsureCoreWebView2Async();
                WebView.Source = new Uri(vm.WebSource);
            }
        }
    }
}
