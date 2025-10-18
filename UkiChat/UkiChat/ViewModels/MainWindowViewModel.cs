using System;
using Microsoft.AspNetCore.SignalR;
using Prism.Commands;
using Prism.Events;
using UkiChat.Configuration;
using UkiChat.Events;
using UkiChat.Hubs;
using UkiChat.Services;

namespace UkiChat.ViewModels;

public class MainWindowViewModel
{
    private readonly IWindowService _windowService;
    private readonly ILocalizationService _localizationService;
    public DelegateCommand OpenSettingsCommand { get; }

    public MainWindowViewModel(IWindowService windowService
        , IDatabaseContext databaseContext
        , ILocalizationService localizationService
        )
    {
        _windowService = windowService;
        _localizationService = localizationService;
        var twitchGlobalSettings = databaseContext.TwitchGlobalSettingsRepository.Get();
        var defaultProfile = databaseContext.ProfileRepository.GetDefaultProfile();
        Console.WriteLine(twitchGlobalSettings.Id);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotUsername);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
        Console.WriteLine(defaultProfile.Id);
        Console.WriteLine(defaultProfile.Name);
        OpenSettingsCommand = new DelegateCommand(OnOpenSettingsWindow);
        //hubContext.Clients.All.SendAsync("ReceiveMessage", "Hello from the server");
    }

    private void OnOpenSettingsWindow()
    {
        _windowService.ShowSettingsWindow("Test");
    }
}