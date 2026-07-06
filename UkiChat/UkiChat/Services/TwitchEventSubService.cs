using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;
using UkiChat.Configuration;
using UkiChat.Model.Chat;

namespace UkiChat.Services;

public class TwitchEventSubService : ITwitchEventSubService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TwitchEventSubService> _logger;
    private readonly IDatabaseContext _databaseContext;
    private readonly ITwitchApiService _twitchApiService;
    private readonly ISignalRService _signalRService;

    private EventSubWebsocketClient? _client;
    private string _broadcasterId = "";
    private string _accessToken = "";
    private bool _stopping;

    public TwitchEventSubService(
        ILoggerFactory loggerFactory,
        ILogger<TwitchEventSubService> logger,
        IDatabaseContext databaseContext,
        ITwitchApiService twitchApiService,
        ISignalRService signalRService)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _databaseContext = databaseContext;
        _twitchApiService = twitchApiService;
        _signalRService = signalRService;
    }

    public async Task StartAsync()
    {
        if (_client != null)
            return; // уже запущено

        var settings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(settings.UserId) || string.IsNullOrEmpty(settings.UserAccessToken))
        {
            _logger.LogDebug("EventSub: пользователь не авторизован — старт пропущен");
            return;
        }

        _broadcasterId = settings.UserId;
        _accessToken = settings.UserAccessToken;
        _stopping = false;

        _client = new EventSubWebsocketClient(_loggerFactory);
        _client.WebsocketConnected += OnWebsocketConnected;
        _client.WebsocketDisconnected += OnWebsocketDisconnected;
        _client.ErrorOccurred += OnErrorOccurred;
        _client.ChannelPointsCustomRewardRedemptionAdd += OnRedemptionAdd;

        if (!await _client.ConnectAsync())
        {
            // Первое подключение не удалось (например, DNS ещё не поднялся после старта ПК) —
            // повторяем в фоне, иначе EventSub молча остаётся мёртвым на всю сессию.
            _logger.LogWarning("EventSub: подключение не удалось — повторяем в фоне");
            var client = _client;
            _ = Task.Run(() => ReconnectLoopAsync(client));
            return;
        }

        _logger.LogInformation("EventSub: подключение инициировано для {BroadcasterId}", _broadcasterId);
    }

    public async Task StopAsync()
    {
        if (_client == null)
            return;

        _stopping = true;
        try
        {
            await _client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "EventSub: ошибка при отключении");
        }
        _client = null;
        _logger.LogInformation("EventSub: остановлен");
    }

    public async Task RestartAsync()
    {
        await StopAsync();
        await StartAsync();
    }

    private async Task OnWebsocketConnected(object? sender, WebsocketConnectedArgs e)
    {
        // При переподключении (reconnect) подписки сохраняются — заново подписываться не нужно.
        if (e.IsRequestedReconnect || _client == null)
            return;

        try
        {
            await _twitchApiService.CreateChannelPointsRedemptionSubscriptionAsync(
                _broadcasterId, _client.SessionId, _accessToken);
            _logger.LogInformation("EventSub: подписка на активации наград создана");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventSub: не удалось создать подписку на награды");
        }
    }

    private async Task OnWebsocketDisconnected(object? sender, WebsocketDisconnectedArgs e)
    {
        if (_stopping || _client == null)
            return;

        _logger.LogWarning("EventSub: соединение потеряно, переподключаемся");
        await ReconnectLoopAsync(_client);
    }

    /// <summary>
    ///     Повторяет ReconnectAsync (клиент пересоздаёт WebSocket внутри) до успеха.
    ///     Прекращается при остановке сервиса или замене клиента (RestartAsync).
    /// </summary>
    private async Task ReconnectLoopAsync(EventSubWebsocketClient client)
    {
        try
        {
            while (!_stopping && ReferenceEquals(_client, client) && !await client.ReconnectAsync())
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EventSub: цикл переподключения прерван ошибкой");
        }
    }

    private Task OnErrorOccurred(object? sender, ErrorOccuredArgs e)
    {
        _logger.LogError(e.Exception, "EventSub: ошибка соединения");
        return Task.CompletedTask;
    }

    private async Task OnRedemptionAdd(object? sender, ChannelPointsCustomRewardRedemptionArgs e)
    {
        var redemption = e.Payload.Event;

        // Награды С вводом текста приходят и через IRC (там они отображаются с эмоутами/бейджами).
        // Здесь обрабатываем только награды БЕЗ текста, чтобы не было дублей.
        if (!string.IsNullOrWhiteSpace(redemption.UserInput))
            return;

        _logger.LogDebug("EventSub: награда без текста {Title} от {User}",
            redemption.Reward.Title, redemption.UserName);

        await _signalRService.SendChatMessageAsync(
            UkiChatMessage.FromTwitchChannelPointsRedemption(
                redemption.UserName, redemption.Reward.Title, redemption.Reward.Cost));
    }
}
