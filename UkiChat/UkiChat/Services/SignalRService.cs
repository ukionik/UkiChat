using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using UkiChat.Hubs;
using UkiChat.Model.Chat;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public class SignalRService(IHubContext<AppHub> hubContext) : ISignalRService
{
    public async Task SendChatMessageAsync(UkiChatMessage message)
    {
        await hubContext.Clients.All.SendAsync("OnChatMessage", message);
    }

    public async Task SendMessageDeletedAsync(string messageId)
    {
        await hubContext.Clients.All.SendAsync("OnMessageDeleted", messageId);
    }

    public async Task SendUserMessagesDeletedAsync(string username)
    {
        await hubContext.Clients.All.SendAsync("OnUserMessagesDeleted", username);
    }

    public async Task SendTwitchReconnect()
    {
        await hubContext.Clients.All.SendAsync("OnTwitchReconnect");
    }

    public async Task SendVkVideoLiveReconnect()
    {
        await hubContext.Clients.All.SendAsync("OnVkVideoLiveReconnect");
    }

    public async Task SendYouTubeReconnect()
    {
        await hubContext.Clients.All.SendAsync("OnYouTubeReconnect");
    }

    public async Task SendTwitchAuthChanged(TwitchAuthStatusData status)
    {
        await hubContext.Clients.All.SendAsync("OnTwitchAuthChanged", status);
    }

    public async Task SendDonationAlertsAuthChanged(DonationAlertsAuthStatusData status)
    {
        await hubContext.Clients.All.SendAsync("OnDonationAlertsAuthChanged", status);
    }
}