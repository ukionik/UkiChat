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
    private readonly IWindowService _windowService = ContainerLocator.Container.Resolve<IWindowService>();
    
    public async Task OpenSettingsWindow()
    {
        _windowService.ShowWindow<SettingsWindow>();        
    }

    /*public async Task ChangeLanguage(string culture)
    {
        _localizationService.SetCulture(culture);
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", $"{culture}.json");
        var json = await File.ReadAllTextAsync(filePath);
        await Clients.All.SendAsync("LanguageChanged", culture, json);
    }*/
    
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
    }
}