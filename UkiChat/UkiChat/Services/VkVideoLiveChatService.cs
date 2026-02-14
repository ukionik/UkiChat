using System.Threading.Tasks;
using UkiChat.Model.VkVideoLive;

namespace UkiChat.Services;

public class VkVideoLiveChatService : IVkVideoLiveChatService
{
    public Task ConnectAsync(VkVideoLiveConnectionParams connectionParams)
    {
        throw new System.NotImplementedException();
    }

    public Task ChangeChannelAsync(string newChannel)
    {
        throw new System.NotImplementedException();
    }

    public Task LoadGlobalDataAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task LoadChannelDataAsync()
    {
        throw new System.NotImplementedException();
    }
}