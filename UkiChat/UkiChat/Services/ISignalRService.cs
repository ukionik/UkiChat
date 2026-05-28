using System.Threading.Tasks;
using UkiChat.Model.Chat;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public interface ISignalRService
{
    Task SendChatMessageAsync(UkiChatMessage message);
    Task SendMessageDeletedAsync(string messageId);
    Task SendUserMessagesDeletedAsync(string username);
    Task SendTwitchReconnect();
    Task SendVkVideoLiveReconnect();
    Task SendTwitchAuthChanged(TwitchAuthStatusData status);
}