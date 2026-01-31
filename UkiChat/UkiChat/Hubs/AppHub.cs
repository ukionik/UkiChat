using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Prism.Ioc;
using UkiChat.Model.Chat;
using UkiChat.Model.Info;
using UkiChat.Model.Settings;
using UkiChat.Services;

namespace UkiChat.Hubs;

public class AppHub : Hub
{
    private readonly IDatabaseService _databaseService = ContainerLocator.Container.Resolve<IDatabaseService>();
    private readonly ISignalRService _signalRService = ContainerLocator.Container.Resolve<ISignalRService>();
    private readonly IStreamService _streamService = ContainerLocator.Container.Resolve<IStreamService>();
    private readonly IWindowService _windowService = ContainerLocator.Container.Resolve<IWindowService>();

    public async Task OpenSettingsWindow()
    {
        _windowService.ShowWindow<SettingsWindow>();
    }

    public async Task ConnectToTwitch()
    {
        await _streamService.ConnectToTwitchAsync();
    }

    public async Task ConnectToVkVideoLive()
    {
        await _streamService.ConnectToVkVideoLiveAsync();
    }

    public async Task<string> GetLanguage(string language)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", $"{language}.json");
        return await File.ReadAllTextAsync(filePath);
    }

    public async Task<AppSettingsInfoData> GetActiveAppSettingsInfo()
    {
        return _databaseService.GetActiveAppSettingsInfo();
    }

    public async Task<AppSettingsData> GetActiveAppSettingsData()
    {
        return _databaseService.GetActiveAppSettingsData();
    }

    public async Task UpdateTwitchSettings(TwitchSettingsData settings)
    {
        _databaseService.UpdateTwitchSettings(settings);
        await _signalRService.SendTwitchReconnect();
    }

    public async Task SendChatMessage(UkiChatMessage chatMessage)
    {
        await _signalRService.SendChatMessageAsync(chatMessage);
    }
}