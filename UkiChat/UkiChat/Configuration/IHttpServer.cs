using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using UkiChat.Hubs;

namespace UkiChat.Configuration;

public interface IHttpServer
{
    public IHubContext<AppHub> HubContext { get; }
    public Task StartAsync();
    public Task StopAsync();
}