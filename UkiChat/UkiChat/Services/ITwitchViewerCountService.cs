using System.Threading.Tasks;

namespace UkiChat.Services;

public interface ITwitchViewerCountService
{
    void Start();
    Task PollNowAsync();
}
