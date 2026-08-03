using System.Threading.Tasks;

namespace UkiChat.Services;

public interface IYouTubeViewerCountService
{
    void Start();
    Task PollNowAsync();
}
