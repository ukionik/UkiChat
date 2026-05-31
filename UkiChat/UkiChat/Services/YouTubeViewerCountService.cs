using System;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using UkiChat.Configuration;
using UkiChat.Events;

namespace UkiChat.Services;

public class YouTubeViewerCountService : IYouTubeViewerCountService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly IEventAggregator _eventAggregator;
    private readonly IYouTubeApiService _youTubeApiService;

    public YouTubeViewerCountService(
        IDatabaseContext databaseContext,
        IEventAggregator eventAggregator,
        IYouTubeApiService youTubeApiService)
    {
        _databaseContext = databaseContext;
        _eventAggregator = eventAggregator;
        _youTubeApiService = youTubeApiService;
    }

    public void Start()
    {
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        await PollAsync();
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync())
            await PollAsync();
    }

    private async Task PollAsync()
    {
        try
        {
            var settings = _databaseContext.YouTubeSettingsRepository.GetActiveSettings();
            if (string.IsNullOrEmpty(settings.Channel))
                return;

            var viewerCount = await _youTubeApiService.GetViewerCountAsync(settings.Channel);
            _eventAggregator.GetEvent<YouTubeViewerCountUpdatedEvent>().Publish(viewerCount);
        }
        catch (Exception)
        {
            // Не прерываем цикл при ошибке сети
        }
    }
}
