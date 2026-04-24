using System;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using UkiChat.Configuration;
using UkiChat.Events;

namespace UkiChat.Services;

public class TwitchViewerCountService : ITwitchViewerCountService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly IEventAggregator _eventAggregator;
    private readonly ITwitchApiService _twitchApiService;

    public TwitchViewerCountService(
        IDatabaseContext databaseContext,
        IEventAggregator eventAggregator,
        ITwitchApiService twitchApiService)
    {
        _databaseContext = databaseContext;
        _eventAggregator = eventAggregator;
        _twitchApiService = twitchApiService;
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
            var settings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
            if (string.IsNullOrEmpty(settings.ApiClientId)
                || string.IsNullOrEmpty(settings.ApiAccessToken)
                || string.IsNullOrEmpty(settings.Channel))
                return;

            await _twitchApiService.InitializeAsync(settings.ApiClientId, settings.ApiAccessToken);
            var viewerCount = await _twitchApiService.GetViewerCountAsync(settings.Channel);

            _eventAggregator.GetEvent<TwitchViewerCountUpdatedEvent>().Publish(viewerCount);
        }
        catch (Exception)
        {
            // Не прерываем цикл при ошибке сети или API
        }
    }
}
