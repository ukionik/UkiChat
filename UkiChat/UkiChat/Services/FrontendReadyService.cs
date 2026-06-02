using System.Threading.Tasks;

namespace UkiChat.Services;

public class FrontendReadyService : IFrontendReadyService
{
    private readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitAsync() => _tcs.Task;
    public void NotifyReady() => _tcs.TrySetResult();
}
