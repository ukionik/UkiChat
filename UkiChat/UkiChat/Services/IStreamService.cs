using System.Threading.Tasks;

namespace UkiChat.Services;

public interface IStreamService
{
    Task ConnectToTwitchAsync(string channel);
}