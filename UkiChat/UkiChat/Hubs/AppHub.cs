using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Prism.Events;
using Prism.Ioc;
using UkiChat.Events;
using UkiChat.Model.Settings;
using UkiChat.Services;

namespace UkiChat.Hubs;

public class AppHub : Hub
{
    private readonly IEventAggregator _eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
    private readonly ILocalizationService _localizationService = ContainerLocator.Container.Resolve<ILocalizationService>();
    private readonly IDatabaseService _databaseService = ContainerLocator.Container.Resolve<IDatabaseService>();

    public async Task SendMessage(string user, string message)
    {
        // Отправка всем клиентам
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
    
    public void OpenSettingsWindow()
    {
        _eventAggregator.GetEvent<OpenSettingsWindowEvent>().Publish("Settings");
    }

    public async Task ChangeLanguage(string culture)
    {
        _localizationService.SetCulture(culture);
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", $"{culture}.json");
        var json = await File.ReadAllTextAsync(filePath);
        await Clients.All.SendAsync("LanguageChanged", culture, json);
    }

    public async Task GetCurrentLanguage()
    {
        var culture = "ru";
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", $"{culture}.json");
        var json = await File.ReadAllTextAsync(filePath);
        await Clients.Caller.SendAsync("LanguageChanged", culture, json);
    }

    public async Task UpdateTwitchSettings(TwitchSettingsData settings)
    {
        _databaseService.UpdateTwitchSettings(settings);
    }
    
    public async Task<AppSettingsInfoData> GetActiveAppSettingsInfo()
    {   
        return _databaseService.GetActiveAppSettingsInfo();
    }
}