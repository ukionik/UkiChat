using System;
using Microsoft.AspNetCore.SignalR;
using Prism.Events;
using UkiChat.Configuration;
using UkiChat.Events;
using UkiChat.Hubs;
using UkiChat.Services;

namespace UkiChat.ViewModels;

public class MainWindowViewModel
{
    private readonly IWindowService _windowService;

    public MainWindowViewModel(IEventAggregator eventAggregator
        , IWindowService windowService
        , IDatabaseContext databaseContext
        )
    {
        _windowService = windowService;
        eventAggregator.GetEvent<OpenSettingsWindowEvent>().Subscribe(OnOpenSettingsWindow);
        var twitchGlobalSettings = databaseContext.TwitchGlobalSettingsRepository.Get();
        var defaultProfile = databaseContext.ProfileRepository.GetDefaultProfile();
        Console.WriteLine(twitchGlobalSettings.Id);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotUsername);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
        Console.WriteLine(twitchGlobalSettings.TwitchChatBotAccessToken);
        Console.WriteLine(defaultProfile.Id);
        Console.WriteLine(defaultProfile.Name);
        //hubContext.Clients.All.SendAsync("ReceiveMessage", "Hello from the server");
    }

    private void OnOpenSettingsWindow(string message)
    {
        _windowService.ShowSettingsWindow(message);
    }
}