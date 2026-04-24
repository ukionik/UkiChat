using System;
using System.Windows;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using UkiChat.Configuration;
using UkiChat.Events;
using UkiChat.Services;

namespace UkiChat.ViewModels;

public class MainWindowViewModel : BindableBase
{
    private readonly string _webSource;
    private readonly IWindowService _windowService;
    private readonly ILocalizationService _localizationService;

    private bool _hasReceivedTwitchCount;
    private int? _lastTwitchViewerCount;

    private bool _hasReceivedVkCount;
    private int? _lastVkViewerCount;

    public MainWindowViewModel(IWindowService windowService
        , IDatabaseContext databaseContext
        , ITwitchViewerCountService twitchViewerCountService
        , IVkVideoLiveViewerCountService vkViewerCountService
        , IEventAggregator eventAggregator
        , ILocalizationService localizationService
    )
    {
        _windowService = windowService;
        _localizationService = localizationService;
        OpenProfileWindowCommand = new DelegateCommand(OpenProfileWindow);
        OpenSettingsWindowCommand = new DelegateCommand(OnOpenSettingsWindow);
        WebSource = $"http://localhost:5000?ts={DateTime.Now.Ticks}";

        eventAggregator.GetEvent<TwitchViewerCountUpdatedEvent>()
            .Subscribe(count =>
            {
                _hasReceivedTwitchCount = true;
                _lastTwitchViewerCount = count;
                UpdateTwitchViewerCountDisplay();
            }, ThreadOption.UIThread);

        eventAggregator.GetEvent<VkVideoLiveViewerCountUpdatedEvent>()
            .Subscribe(count =>
            {
                _hasReceivedVkCount = true;
                _lastVkViewerCount = count;
                UpdateVkViewerCountDisplay();
            }, ThreadOption.UIThread);

        localizationService.LanguageChanged += (_, _) => Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateTwitchViewerCountDisplay();
            UpdateVkViewerCountDisplay();
        });

        twitchViewerCountService.Start();
        vkViewerCountService.Start();
    }

    public DelegateCommand OpenProfileWindowCommand { get; }
    public DelegateCommand OpenSettingsWindowCommand { get; }

    public string WebSource
    {
        get => _webSource;
        init => SetProperty(ref _webSource, value);
    }

    private string _twitchViewerCount = "—";
    public string TwitchViewerCount
    {
        get => _twitchViewerCount;
        set => SetProperty(ref _twitchViewerCount, value);
    }

    private string _vkVideoLiveViewerCount = "—";
    public string VkVideoLiveViewerCount
    {
        get => _vkVideoLiveViewerCount;
        set => SetProperty(ref _vkVideoLiveViewerCount, value);
    }

    private void UpdateTwitchViewerCountDisplay()
    {
        if (!_hasReceivedTwitchCount)
            return;

        TwitchViewerCount = _lastTwitchViewerCount.HasValue
            ? _lastTwitchViewerCount.Value.ToString()
            : _localizationService.GetString("twitch.offline");
    }

    private void UpdateVkViewerCountDisplay()
    {
        if (!_hasReceivedVkCount)
            return;

        VkVideoLiveViewerCount = _lastVkViewerCount.HasValue
            ? _lastVkViewerCount.Value.ToString()
            : _localizationService.GetString("vkvideolive.offline");
    }

    private void OpenProfileWindow()
    {
        _windowService.ShowWindow<ProfileWindow>();
    }

    private void OnOpenSettingsWindow()
    {
        _windowService.ShowWindow<SettingsWindow>();
    }
}