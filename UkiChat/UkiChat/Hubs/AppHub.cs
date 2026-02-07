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
    private readonly ITwitchChatService _twitchChatService = ContainerLocator.Container.Resolve<ITwitchChatService>();
    private readonly IWindowService _windowService = ContainerLocator.Container.Resolve<IWindowService>();

    public Task OpenSettingsWindow()
    {
        _windowService.ShowWindow<SettingsWindow>();
        return Task.CompletedTask;
    }
    
    public async Task<string> GetLanguage(string language)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", $"{language}.json");
        return await File.ReadAllTextAsync(filePath);
    }

    public Task<AppSettingsInfoData> GetActiveAppSettingsInfo()
    {
        return Task.FromResult(_databaseService.GetActiveAppSettingsInfo());
    }

    public Task<AppSettingsData> GetActiveAppSettingsData()
    {
        return Task.FromResult(_databaseService.GetActiveAppSettingsData());
    }

    public async Task ChangeTwitchChannel(string newChannel)
    {
        await _twitchChatService.ChangeChannelAsync(newChannel);
    }

    public async Task UpdateTwitchSettings(TwitchSettingsData settings)
    {
        _databaseService.UpdateTwitchSettings(settings);
        await _signalRService.SendTwitchReconnect();
    }
    
    public async Task UpdateVkVideoLiveSettings(VkVideoLiveSettingsData settings)
    {
        _databaseService.UpdateVkVideoLiveSettings(settings);
        await _signalRService.SendVkVideoLiveReconnect();
    }
    
    public async Task SendChatMessage(UkiChatMessage chatMessage)
    {
        await _signalRService.SendChatMessageAsync(chatMessage);
    }
}