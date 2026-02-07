using System.Threading.Tasks;

namespace UkiChat.Core;

public interface IChatService
{
    Task ConnectAsync();
}