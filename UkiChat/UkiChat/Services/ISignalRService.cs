using System.Threading.Tasks;
using UkiChat.Model.Chat;

namespace UkiChat.Services;

public interface ISignalRService
{
    Task SendChatMessageAsync(UkiChatMessage message);
    Task SendTwitchReconnect();
}