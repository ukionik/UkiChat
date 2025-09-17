using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace UkiChat.Hubs;

public class AppHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        // Отправка всем клиентам
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }    
}