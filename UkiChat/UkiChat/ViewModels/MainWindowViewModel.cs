using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
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

    private bool _hasReceivedYouTubeCount;
    private int? _lastYouTubeViewerCount;

    private DateTime? _twitchStreamStartedAt;
    private DispatcherTimer? _streamUptimeTimer;

    public MainWindowViewModel(IWindowService windowService
        , IDatabaseContext databaseContext
        , ITwitchViewerCountService twitchViewerCountService
        , IVkVideoLiveViewerCountService vkViewerCountService
        , IYouTubeViewerCountService youTubeViewerCountService
        , IEventAggregator eventAggregator
        , ILocalizationService localizationService
    )
    {
        _windowService = windowService;
        _localizationService = localizationService;
        OpenProfileWindowCommand = new DelegateCommand(OpenProfileWindow);
        OpenSettingsWindowCommand = new DelegateCommand(OnOpenSettingsWindow);
        ToggleTopMostCommand = new DelegateCommand(() => IsTopMost = !IsTopMost);
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

        eventAggregator.GetEvent<YouTubeViewerCountUpdatedEvent>()
            .Subscribe(count =>
            {
                _hasReceivedYouTubeCount = true;
                _lastYouTubeViewerCount = count;
                UpdateYouTubeViewerCountDisplay();
            }, ThreadOption.UIThread);

        eventAggregator.GetEvent<TwitchStreamStartedAtUpdatedEvent>()
            .Subscribe(startedAt =>
            {
                _twitchStreamStartedAt = startedAt;
                if (startedAt.HasValue)
                    StartUptimeTimer();
                else
                    StopUptimeTimer();
            }, ThreadOption.UIThread);

        localizationService.LanguageChanged += (_, _) => Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateTwitchViewerCountDisplay();
            UpdateVkViewerCountDisplay();
            UpdateYouTubeViewerCountDisplay();
            UpdateTotalViewerCountDisplay();
        });

        twitchViewerCountService.Start();
        vkViewerCountService.Start();
        youTubeViewerCountService.Start();
    }

    public DelegateCommand OpenProfileWindowCommand { get; }
    public DelegateCommand OpenSettingsWindowCommand { get; }
    public DelegateCommand ToggleTopMostCommand { get; }

    public string AppVersion { get; } = BuildAppVersion();

    private bool _isTopMost;
    public bool IsTopMost
    {
        get => _isTopMost;
        set => SetProperty(ref _isTopMost, value);
    }

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

    private string _youTubeViewerCount = "—";
    public string YouTubeViewerCount
    {
        get => _youTubeViewerCount;
        set => SetProperty(ref _youTubeViewerCount, value);
    }

    private string _totalViewerCount = "—";
    public string TotalViewerCount
    {
        get => _totalViewerCount;
        set => SetProperty(ref _totalViewerCount, value);
    }

    private string _streamUptimeDisplay = "";
    public string StreamUptimeDisplay
    {
        get => _streamUptimeDisplay;
        set => SetProperty(ref _streamUptimeDisplay, value);
    }

    private Visibility _streamUptimeVisibility = Visibility.Collapsed;
    public Visibility StreamUptimeVisibility
    {
        get => _streamUptimeVisibility;
        set => SetProperty(ref _streamUptimeVisibility, value);
    }

    private void UpdateTwitchViewerCountDisplay()
    {
        if (!_hasReceivedTwitchCount)
            return;

        TwitchViewerCount = _lastTwitchViewerCount.HasValue
            ? _lastTwitchViewerCount.Value.ToString()
            : _localizationService.GetString("twitch.offline");

        UpdateTotalViewerCountDisplay();
    }

    private void UpdateVkViewerCountDisplay()
    {
        if (!_hasReceivedVkCount)
            return;

        VkVideoLiveViewerCount = _lastVkViewerCount.HasValue
            ? _lastVkViewerCount.Value.ToString()
            : _localizationService.GetString("vkvideolive.offline");

        UpdateTotalViewerCountDisplay();
    }

    private void UpdateYouTubeViewerCountDisplay()
    {
        if (!_hasReceivedYouTubeCount)
            return;

        YouTubeViewerCount = _lastYouTubeViewerCount.HasValue
            ? _lastYouTubeViewerCount.Value.ToString()
            : _localizationService.GetString("youtube.offline");

        UpdateTotalViewerCountDisplay();
    }

    private void UpdateTotalViewerCountDisplay()
    {
        if (!_hasReceivedTwitchCount && !_hasReceivedVkCount && !_hasReceivedYouTubeCount)
            return;

        var total = (_lastTwitchViewerCount ?? 0) + (_lastVkViewerCount ?? 0) + (_lastYouTubeViewerCount ?? 0);
        TotalViewerCount = total.ToString();
    }

    private void StartUptimeTimer()
    {
        if (_streamUptimeTimer == null)
        {
            _streamUptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _streamUptimeTimer.Tick += (_, _) => UpdateStreamUptimeDisplay();
        }
        if (!_streamUptimeTimer.IsEnabled)
            _streamUptimeTimer.Start();
        UpdateStreamUptimeDisplay();
        StreamUptimeVisibility = Visibility.Visible;
    }

    private void StopUptimeTimer()
    {
        _streamUptimeTimer?.Stop();
        StreamUptimeVisibility = Visibility.Collapsed;
    }

    private void UpdateStreamUptimeDisplay()
    {
        if (!_twitchStreamStartedAt.HasValue) return;
        var elapsed = DateTime.UtcNow - _twitchStreamStartedAt.Value;
        StreamUptimeDisplay = elapsed.ToString(@"h\:mm\:ss");
    }

    private static string BuildAppVersion()
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version!;
        return $"{v.Major}.{v.Minor}.{v.Build}";
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