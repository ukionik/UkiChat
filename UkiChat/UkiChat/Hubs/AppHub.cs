using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Prism.Ioc;
using UkiChat.Diagnostics;
using UkiChat.Model.Chat;
using UkiChat.Model.Info;
using UkiChat.Model.Settings;
using UkiChat.Services;

namespace UkiChat.Hubs;

public class AppHub : Hub
{
    private readonly IDatabaseService _databaseService = ContainerLocator.Container.Resolve<IDatabaseService>();
    private readonly ISignalRService _signalRService = ContainerLocator.Container.Resolve<ISignalRService>();
    private readonly ITwitchChatService _twitchChatService = ContainerLocator.Container.Resolve<ITwitchChatService>();
    private readonly IVkVideoLiveChatService _vkVideoLiveChatService = ContainerLocator.Container.Resolve<IVkVideoLiveChatService>();
    private readonly IWindowService _windowService = ContainerLocator.Container.Resolve<IWindowService>();
    private readonly ITwitchAuthService _twitchAuthService = ContainerLocator.Container.Resolve<ITwitchAuthService>();

    public override Task OnConnectedAsync()
    {
        var ctx = Context.GetHttpContext();
        var remote = ctx?.Connection.RemoteIpAddress?.ToString() ?? "?";
        var userAgent = ctx?.Request.Headers["User-Agent"].ToString() ?? "?";
        StartupDiagnostics.Log("hub", $"OnConnectedAsync: connId={Context.ConnectionId} remote={remote} ua=\"{userAgent}\"");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            StartupDiagnostics.LogError("hub", $"OnDisconnectedAsync: connId={Context.ConnectionId}", exception);
        else
            StartupDiagnostics.Log("hub", $"OnDisconnectedAsync: connId={Context.ConnectionId}");
        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>Метод для логирования с фронта (через SignalR). Бэк просто пишет в startup-лог.</summary>
    public Task LogClient(string level, string message)
    {
        StartupDiagnostics.Log($"front:{level}", message);
        return Task.CompletedTask;
    }

    public Task OpenSettingsWindow()
    {
        return Measure(nameof(OpenSettingsWindow), () =>
        {
            _windowService.ShowWindow<SettingsWindow>();
            return Task.CompletedTask;
        });
    }

    public Task OpenUrl(string url)
    {
        return Measure(nameof(OpenUrl), () =>
        {
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            return Task.CompletedTask;
        });
    }

    /// <summary>Открывает системный браузер на странице авторизации Twitch.</summary>
    public Task StartTwitchAuth()
    {
        return Measure(nameof(StartTwitchAuth), () =>
        {
            var url = _twitchAuthService.BuildAuthorizeUrl();
            if (!string.IsNullOrEmpty(url))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return Task.CompletedTask;
        });
    }

    public Task<TwitchAuthStatusData> GetTwitchAuthStatus()
    {
        return MeasureResult(nameof(GetTwitchAuthStatus),
            () => Task.FromResult(_twitchAuthService.GetStatus()));
    }

    public Task LogoutTwitch()
    {
        return Measure(nameof(LogoutTwitch), () => _twitchAuthService.LogoutAsync());
    }

    public async Task<string> GetLanguage(string language)
    {
        var sw = Stopwatch.StartNew();
        StartupDiagnostics.Log("hub", $"GetLanguage({language}): BEGIN connId={Context.ConnectionId}");
        try
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", $"{language}.json");
            var content = await File.ReadAllTextAsync(filePath);
            sw.Stop();
            StartupDiagnostics.Log("hub", $"GetLanguage({language}): END took={sw.ElapsedMilliseconds} ms size={content.Length}");
            return content;
        }
        catch (Exception ex)
        {
            sw.Stop();
            StartupDiagnostics.LogError("hub", $"GetLanguage({language}) FAILED took={sw.ElapsedMilliseconds} ms", ex);
            throw;
        }
    }

    public Task<AppSettingsInfoData> GetActiveAppSettingsInfo()
    {
        return MeasureResult(nameof(GetActiveAppSettingsInfo),
            () => Task.FromResult(_databaseService.GetActiveAppSettingsInfo()));
    }

    public Task<AppSettingsData> GetActiveAppSettingsData()
    {
        return MeasureResult(nameof(GetActiveAppSettingsData),
            () => Task.FromResult(_databaseService.GetActiveAppSettingsData()));
    }

    public Task ChangeTwitchChannel(string newChannel)
    {
        return Measure($"{nameof(ChangeTwitchChannel)}({newChannel})",
            () => _twitchChatService.ChangeChannelAsync(newChannel));
    }

    public Task ChangeVkVideoLiveChannel(string newChannel)
    {
        return Measure($"{nameof(ChangeVkVideoLiveChannel)}({newChannel})",
            () => _vkVideoLiveChatService.ChangeChannelAsync(newChannel));
    }

    public Task UpdateTwitchSettings(TwitchSettingsData settings)
    {
        return Measure(nameof(UpdateTwitchSettings), async () =>
        {
            _databaseService.UpdateTwitchSettings(settings);
            await _signalRService.SendTwitchReconnect();
        });
    }

    public Task UpdateVkVideoLiveSettings(VkVideoLiveSettingsData settings)
    {
        return Measure(nameof(UpdateVkVideoLiveSettings), async () =>
        {
            _databaseService.UpdateVkVideoLiveSettings(settings);
            await _signalRService.SendVkVideoLiveReconnect();
        });
    }

    public Task<ScaleSettingsData> GetScaleSettings()
    {
        return MeasureResult(nameof(GetScaleSettings),
            () => Task.FromResult(_databaseService.GetScaleSettings()));
    }

    public Task BroadcastScaleSettings(int mainWindowScale, int overlayScale)
    {
        return Measure(nameof(BroadcastScaleSettings), async () =>
        {
            _databaseService.UpdateScaleSettings(new ScaleSettingsData(mainWindowScale, overlayScale));
            await Clients.All.SendAsync("OnScaleSettingsChanged", mainWindowScale, overlayScale);
        });
    }

    public Task<ThemeSettingsData> GetThemeSettings()
    {
        return MeasureResult(nameof(GetThemeSettings),
            () => Task.FromResult(_databaseService.GetThemeSettings()));
    }

    public Task BroadcastThemeSettings(string mainWindowTheme, string overlayTheme)
    {
        return Measure(nameof(BroadcastThemeSettings), async () =>
        {
            _databaseService.UpdateThemeSettings(new ThemeSettingsData(mainWindowTheme, overlayTheme));
            await Clients.All.SendAsync("OnThemeSettingsChanged", mainWindowTheme, overlayTheme);
        });
    }

    public Task<MessageHideSettingsData> GetMessageHideSettings()
    {
        return MeasureResult(nameof(GetMessageHideSettings),
            () => Task.FromResult(_databaseService.GetMessageHideSettings()));
    }

    public Task BroadcastMessageHideSettings(int mainWindowMessageHideDelay, int overlayMessageHideDelay)
    {
        return Measure(nameof(BroadcastMessageHideSettings), async () =>
        {
            _databaseService.UpdateMessageHideSettings(new MessageHideSettingsData(mainWindowMessageHideDelay, overlayMessageHideDelay));
            await Clients.All.SendAsync("OnMessageHideSettingsChanged", mainWindowMessageHideDelay, overlayMessageHideDelay);
        });
    }

    public Task SendChatMessage(UkiChatMessage chatMessage)
    {
        return Measure(nameof(SendChatMessage),
            () => _signalRService.SendChatMessageAsync(chatMessage));
    }

    private async Task Measure(string method, Func<Task> action)
    {
        var sw = Stopwatch.StartNew();
        StartupDiagnostics.Log("hub", $"{method}: BEGIN connId={Context.ConnectionId}");
        try
        {
            await action();
            sw.Stop();
            StartupDiagnostics.Log("hub", $"{method}: END took={sw.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            sw.Stop();
            StartupDiagnostics.LogError("hub", $"{method} FAILED took={sw.ElapsedMilliseconds} ms", ex);
            throw;
        }
    }

    private async Task<T> MeasureResult<T>(string method, Func<Task<T>> action)
    {
        var sw = Stopwatch.StartNew();
        StartupDiagnostics.Log("hub", $"{method}: BEGIN connId={Context.ConnectionId}");
        try
        {
            var result = await action();
            sw.Stop();
            StartupDiagnostics.Log("hub", $"{method}: END took={sw.ElapsedMilliseconds} ms");
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            StartupDiagnostics.LogError("hub", $"{method} FAILED took={sw.ElapsedMilliseconds} ms", ex);
            throw;
        }
    }
}
