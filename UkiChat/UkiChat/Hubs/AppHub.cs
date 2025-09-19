using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Prism.Events;
using Prism.Ioc;
using UkiChat.Events;

namespace UkiChat.Hubs;

public class AppHub : Hub
{
    private readonly IEventAggregator _eventAggregator = Prism.Ioc.ContainerLocator.Container.Resolve<IEventAggregator>();

    public async Task SendMessage(string user, string message)
    {
        // Отправка всем клиентам
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
    
    public void OpenSettingsWindow()
    {
        _eventAggregator.GetEvent<OpenSettingsWindowEvent>().Publish("Settings");
    }    

}