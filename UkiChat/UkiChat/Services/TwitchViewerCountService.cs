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

    public Task PollNowAsync() => PollAsync();

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

            // Канал очистили — гасим счётчик и время эфира. Раньше опрос просто выходил, и в
            // статус-баре навсегда зависало последнее значение уже отключённой площадки.
            if (string.IsNullOrEmpty(settings.Channel))
            {
                _eventAggregator.GetEvent<TwitchViewerCountUpdatedEvent>().Publish(null);
                _eventAggregator.GetEvent<TwitchStreamStartedAtUpdatedEvent>().Publish(null);
                return;
            }

            // Helix ходит токеном авторизованного пользователя; без авторизации счётчик молчит.
            if (string.IsNullOrEmpty(settings.ApiClientId) || string.IsNullOrEmpty(settings.UserAccessToken))
                return;

            await _twitchApiService.InitializeAsync(settings.ApiClientId, settings.UserAccessToken);
            var viewerCount = await _twitchApiService.GetViewerCountAsync(settings.Channel);
            _eventAggregator.GetEvent<TwitchViewerCountUpdatedEvent>().Publish(viewerCount);

            if (settings.ShowStreamUptime)
            {
                var startedAt = await _twitchApiService.GetStreamStartedAtAsync(settings.Channel);
                _eventAggregator.GetEvent<TwitchStreamStartedAtUpdatedEvent>().Publish(startedAt);
            }
            else
            {
                _eventAggregator.GetEvent<TwitchStreamStartedAtUpdatedEvent>().Publish(null);
            }
        }
        catch (Exception)
        {
            // Не прерываем цикл при ошибке сети или API
        }
    }
}
