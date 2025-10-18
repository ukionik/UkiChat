using System;
using Prism.Commands;
using Prism.Mvvm;
using UkiChat.Configuration;
using UkiChat.Services;

namespace UkiChat.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private readonly IWindowService _windowService;
    public DelegateCommand OpenProfileWindowCommand { get; }
    public DelegateCommand OpenSettingsWindowCommand { get; }
    private string _webSource;

    public string WebSource
    {
        get => _webSource;
        set => SetProperty(ref _webSource, value);
    }
    

    public MainWindowViewModel(IWindowService windowService
        , IDatabaseContext databaseContext
        )
    {
        _windowService = windowService;
        var twitchGlobalSettings = databaseContext.TwitchGlobalSettingsRepository.Get();
        var defaultProfile = databaseContext.ProfileRepository.GetDefaultProfile();
        Console.WriteLine(twitchGlobalSettings.Id);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotUsername);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
        Console.WriteLine(defaultProfile.Id);
        Console.WriteLine(defaultProfile.Name);
        OpenProfileWindowCommand = new DelegateCommand(OpenProfileWindow);
        OpenSettingsWindowCommand = new DelegateCommand(OnOpenSettingsWindow);
        WebSource = $"http://localhost:5000?ts={DateTime.Now.Ticks}";
        Console.WriteLine($"Web Source: {WebSource}");
    }

    private void OpenProfileWindow()
    {
        _windowService.ShowWindow<ProfileWindow>();        
    }

    private void OnOpenSettingsWindow()
    {
        _windowService.ShowWindow<SettingsWindow>();
    }
}