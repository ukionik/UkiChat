using System.Threading.Tasks;

namespace UkiChat.Core;

public interface IChatService<in TConnectionParams>
{
    Task ConnectAsync(TConnectionParams connectionParams);
    Task ChangeChannelAsync(string newChannel);
    Task LoadGlobalDataAsync();
    Task LoadChannelDataAsync();
}