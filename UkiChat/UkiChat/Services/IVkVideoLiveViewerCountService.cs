using System.Threading.Tasks;

namespace UkiChat.Services;

public interface IVkVideoLiveViewerCountService
{
    void Start();
    Task PollNowAsync();
}
