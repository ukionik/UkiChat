using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Prism.Events;
using Prism.Ioc;
using UkiChat.Events;
using UkiChat.Services;

namespace UkiChat.Hubs;

public class AppHub : Hub
{
    private readonly IEventAggregator _eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
    private readonly ILocalizationService _localizationService = ContainerLocator.Container.Resolve<ILocalizationService>();

    public async Task SendMessage(string user, string message)
    {
        // Отправка всем клиентам
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
    
    public void OpenSettingsWindow()
    {
        _eventAggregator.GetEvent<OpenSettingsWindowEvent>().Publish("Settings");
    }

    public void ChangeLanguage(string culture)
    {
        _localizationService.SetCulture(culture);
    }
}