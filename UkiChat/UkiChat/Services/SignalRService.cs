using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using UkiChat.Hubs;
using UkiChat.Model.Chat;

namespace UkiChat.Services;

public class SignalRService(IHubContext<AppHub> hubContext) : ISignalRService
{
    public async Task SendChatMessageAsync(UkiChatMessage message)
    {
        await hubContext.Clients.All.SendAsync("OnChatMessage", message);
    }
}