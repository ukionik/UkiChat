using System;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using UkiChat.Configuration;
using UkiChat.Events;

namespace UkiChat.Services;

public class VkVideoLiveViewerCountService : IVkVideoLiveViewerCountService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly IEventAggregator _eventAggregator;
    private readonly IVkVideoLiveApiService _vkVideoLiveApiService;

    public VkVideoLiveViewerCountService(
        IDatabaseContext databaseContext,
        IEventAggregator eventAggregator,
        IVkVideoLiveApiService vkVideoLiveApiService)
    {
        _databaseContext = databaseContext;
        _eventAggregator = eventAggregator;
        _vkVideoLiveApiService = vkVideoLiveApiService;
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
            var settings = _databaseContext.VkVideoLiveSettingsRepository.GetActiveSettings();

            // Канал очистили — гасим счётчик, иначе в статус-баре зависает последнее значение.
            if (string.IsNullOrEmpty(settings.Channel))
            {
                _eventAggregator.GetEvent<VkVideoLiveViewerCountUpdatedEvent>().Publish(null);
                return;
            }

            if (string.IsNullOrEmpty(settings.ApiAccessToken))
                return;

            var viewerCount = await _vkVideoLiveApiService.GetViewerCountAsync(settings.ApiAccessToken, settings.Channel);

            _eventAggregator.GetEvent<VkVideoLiveViewerCountUpdatedEvent>().Publish(viewerCount);
        }
        catch (Exception)
        {
            // Не прерываем цикл при ошибке сети или API
        }
    }
}
