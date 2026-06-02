using System.Threading.Tasks;

namespace UkiChat.Services;

public interface IFrontendReadyService
{
    Task WaitAsync();
    void NotifyReady();
}
