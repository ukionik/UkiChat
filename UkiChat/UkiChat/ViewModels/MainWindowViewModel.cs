using System;
using Prism.Commands;
using Prism.Mvvm;
using UkiChat.Configuration;
using UkiChat.Services;

namespace UkiChat.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private readonly string _webSource;
    private readonly IWindowService _windowService;

    public MainWindowViewModel(IWindowService windowService
        , IDatabaseContext databaseContext
    )
    {
        _windowService = windowService;
        var twitchSettings = databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var defaultProfile = databaseContext.ProfileRepository.GetDefaultProfile();
        Console.WriteLine(twitchSettings.Id);
        Console.WriteLine(twitchSettings.ChatbotUsername);
        Console.WriteLine(twitchSettings.ChatbotAccessToken);
        Console.WriteLine(twitchSettings.Channel);
        Console.WriteLine(defaultProfile.Id);
        Console.WriteLine(defaultProfile.Name);
        OpenProfileWindowCommand = new DelegateCommand(OpenProfileWindow);
        OpenSettingsWindowCommand = new DelegateCommand(OnOpenSettingsWindow);
        WebSource = $"http://localhost:5000?ts={DateTime.Now.Ticks}";
    }

    public DelegateCommand OpenProfileWindowCommand { get; }
    public DelegateCommand OpenSettingsWindowCommand { get; }

    public string WebSource
    {
        get => _webSource;
        init => SetProperty(ref _webSource, value);
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