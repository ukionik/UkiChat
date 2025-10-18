using System;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using Prism.Mvvm;
using UkiChat.Configuration;
using UkiChat.Services;

namespace UkiChat.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private readonly string _webSource;
    private readonly IWindowService _windowService;
    private Visibility _visibility = Visibility.Collapsed;

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
        LoadedCommand = new DelegateCommand(OnLoaded);
        WebSource = $"http://localhost:5000?ts={DateTime.Now.Ticks}";
    }

    public DelegateCommand OpenProfileWindowCommand { get; }
    public DelegateCommand OpenSettingsWindowCommand { get; }
    public DelegateCommand LoadedCommand { get; }

    public string WebSource
    {
        get => _webSource;
        init => SetProperty(ref _webSource, value);
    }

    public Visibility Visibility
    {
        get => _visibility;
        set => SetProperty(ref _visibility, value);
    }

    private async void OnLoaded()
    {
        //Костыль на плохую отрисовку
        await Task.Delay(500);
        Visibility = Visibility.Visible;
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