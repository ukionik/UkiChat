using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UkiChat.Model.Chat.EventArgs;
using ErrorEventArgs = UkiChat.Model.Chat.EventArgs.ErrorEventArgs;

namespace UkiChat.Model.DonationAlerts;

/// <summary>
///     Клиент Centrifugo (v2) для DonationAlerts. В отличие от VK канал приватный
///     ($alerts:donation_{userId}): после connect сервер выдаёт client id, под который
///     нужно REST-запросом получить per-channel токен подписки.
/// </summary>
public class DonationAlertsCentrifugeClient : IDisposable
{
    private const string WebSocketUrl = "wss://centrifugo.donationalerts.com/connection/websocket";

    private CancellationTokenSource? _cancellationTokenSource;
    private int _commandId;
    private bool _disposed;
    private ClientWebSocket? _webSocket;
    private string _channel = "";

    // Делегат для получения per-channel токена подписки по client id (REST-запрос).
    private Func<string, Task<string?>>? _subscribeTokenProvider;

    private readonly ILogger<DonationAlertsCentrifugeClient> _logger;

    public DonationAlertsCentrifugeClient(ILogger<DonationAlertsCentrifugeClient> logger)
    {
        _logger = logger;
    }

    public event EventHandler<DonationAlertsDonationEventArgs>? DonationReceived;
    public event EventHandler? Connected;
    public event EventHandler<DisconnectEventArgs>? Disconnected;
    public event EventHandler<ErrorEventArgs>? Error;

    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _webSocket?.Dispose();

        _disposed = true;
    }

    /// <param name="socketConnectionToken">Токен подключения из /api/v1/user/oauth.</param>
    /// <param name="channel">Канал доната, напр. "$alerts:donation_123".</param>
    /// <param name="subscribeTokenProvider">Возвращает токен подписки на приватный канал по client id.</param>
    public async Task ConnectAsync(string socketConnectionToken, string channel,
        Func<string, Task<string?>> subscribeTokenProvider)
    {
        try
        {
            if (_webSocket != null)
                OnDisconnected("Reconnect");

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _webSocket?.Dispose();
            _webSocket = null;

            _channel = channel;
            _subscribeTokenProvider = subscribeTokenProvider;
            _commandId = 0;
            _cancellationTokenSource = new CancellationTokenSource();

            _logger.LogInformation("Подключение к Centrifugo DonationAlerts...");
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cancellationTokenSource.Token);
            _logger.LogInformation("WebSocket подключен");

            _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            await SendConnectCommandAsync(socketConnectionToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка подключения к DonationAlerts");
            OnError($"Ошибка подключения к DonationAlerts: {e.Message}", e);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_cancellationTokenSource is { Token.IsCancellationRequested: false })
                await _cancellationTokenSource.CancelAsync();

            if (_webSocket is { State: WebSocketState.Open })
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect requested",
                    CancellationToken.None);

            _webSocket?.Dispose();
            _webSocket = null;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            OnDisconnected("Disconnect requested");
            _logger.LogInformation("Отключено");
        }
        catch (Exception ex)
        {
            OnError($"Ошибка отключения от DonationAlerts: {ex.Message}", ex);
            throw;
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var messageStream = new MemoryStream();

        try
        {
            while (_webSocket is { State: WebSocketState.Open } && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("Получен Close: {Status} - {Description}",
                        result.CloseStatus, result.CloseStatusDescription);
                    OnDisconnected(result.CloseStatusDescription ?? "Connection closed");
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                messageStream.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                {
                    var messageBytes = messageStream.ToArray();
                    messageStream.SetLength(0);
                    await ProcessJsonMessageAsync(messageBytes);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Получение сообщений отменено");
        }
        catch (ObjectDisposedException)
        {
            _logger.LogInformation("WebSocket освобождён, цикл получения сообщений завершён");
        }
        catch (WebSocketException ex)
        {
            _logger.LogError(ex, "WebSocket ошибка");
            OnError($"WebSocket ошибка: {ex.Message}", ex);
            OnDisconnected($"WebSocket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения сообщений");
            OnError($"Ошибка получения сообщений: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Сервер может прислать несколько JSON-объектов в одном фрейме.
    /// </summary>
    private async Task ProcessJsonMessageAsync(byte[] messageBytes)
    {
        var offset = 0;
        while (offset < messageBytes.Length)
        {
            while (offset < messageBytes.Length && messageBytes[offset] <= 32)
                offset++;

            if (offset >= messageBytes.Length) break;

            try
            {
                JsonElement rootElement;
                int bytesConsumed;
                var reader = new Utf8JsonReader(messageBytes.AsSpan(offset), isFinalBlock: true, state: default);
                if (!reader.Read()) break;
                using (var document = JsonDocument.ParseValue(ref reader))
                {
                    rootElement = document.RootElement.Clone();
                    bytesConsumed = (int)reader.BytesConsumed;
                }

                await ProcessJsonObjectAsync(rootElement);
                offset += bytesConsumed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка парсинга JSON-объекта по смещению {Offset}", offset);
                OnError($"Ошибка обработки сообщения: {ex.Message}", ex);
                break;
            }
        }
    }

    private async Task ProcessJsonObjectAsync(JsonElement root)
    {
        try
        {
            // Пустой объект — ping, отвечаем pong.
            if (root.ValueKind == JsonValueKind.Object && !root.EnumerateObject().MoveNext())
            {
                await SendPongAsync();
                return;
            }

            if (root.TryGetProperty("error", out var errorProp))
            {
                var code = errorProp.TryGetProperty("code", out var codeProp) ? codeProp.GetUInt32() : 0;
                var message = errorProp.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "";
                _logger.LogError("Ошибка сервера [{Code}] {Message}", code, message);
                OnError($"Ошибка сервера [{code}]: {message}");
                return;
            }

            // Ответ на connect: получаем client id и инициируем подписку на приватный канал.
            if (root.TryGetProperty("connect", out var connectProp))
            {
                var clientId = connectProp.TryGetProperty("client", out var clientProp)
                    ? clientProp.GetString() ?? ""
                    : "";
                _logger.LogInformation("Подключено к Centrifugo, client={Client}", clientId);
                OnConnected();
                await SubscribeToChannelAsync(clientId);
            }

            if (root.TryGetProperty("subscribe", out _))
                _logger.LogInformation("Подписка на канал {Channel} успешна", _channel);

            if (root.TryGetProperty("push", out var pushProp))
                ProcessPushMessage(pushProp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки JSON-объекта");
            OnError($"Ошибка обработки JSON-объекта: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Получает per-channel токен через делегат и отправляет команду subscribe.
    /// </summary>
    private async Task SubscribeToChannelAsync(string clientId)
    {
        if (_subscribeTokenProvider == null || string.IsNullOrEmpty(clientId))
            return;

        var channelToken = await _subscribeTokenProvider(clientId);
        if (string.IsNullOrEmpty(channelToken))
        {
            _logger.LogError("Не удалось получить токен подписки на канал {Channel}", _channel);
            OnError($"Не удалось получить токен подписки на канал {_channel}");
            return;
        }

        await SendSubscribeCommandAsync(_channel, channelToken);
    }

    private void ProcessPushMessage(JsonElement push)
    {
        try
        {
            if (push.TryGetProperty("pub", out var pubProp))
            {
                var data = pubProp.TryGetProperty("data", out var dataProp) ? dataProp.GetRawText() : "{}";
                _logger.LogDebug("Получен донат: {Data}", data);
                OnDonationReceived(data);
            }

            if (push.TryGetProperty("unsubscribe", out var unsubProp))
            {
                var reason = unsubProp.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "";
                _logger.LogWarning("Unsubscribe: {Reason}", reason);
            }

            if (push.TryGetProperty("disconnect", out var disconnectProp))
            {
                var reason = disconnectProp.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "";
                _logger.LogWarning("Disconnect push: {Reason}", reason);
                OnDisconnected(reason ?? "");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки push-сообщения");
            OnError($"Ошибка обработки push-сообщения: {ex.Message}", ex);
        }
    }

    private async Task SendConnectCommandAsync(string socketConnectionToken)
    {
        var command = new
        {
            connect = new
            {
                token = socketConnectionToken,
                name = "UkiChat"
            },
            id = ++_commandId
        };

        await SendJsonCommandAsync(JsonSerializer.Serialize(command));
    }

    private async Task SendSubscribeCommandAsync(string channel, string token)
    {
        var command = new
        {
            subscribe = new
            {
                channel,
                token
            },
            id = ++_commandId
        };

        await SendJsonCommandAsync(JsonSerializer.Serialize(command));
    }

    private async Task SendJsonCommandAsync(string json)
    {
        if (_webSocket is not { State: WebSocketState.Open })
            throw new InvalidOperationException("WebSocket не подключен");

        var bytes = Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
            _cancellationTokenSource?.Token ?? CancellationToken.None);
    }

    private async Task SendPongAsync()
    {
        try
        {
            if (_webSocket is not { State: WebSocketState.Open }) return;

            var bytes = Encoding.UTF8.GetBytes("{}");
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
                _cancellationTokenSource?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки pong");
        }
    }

    private void OnDonationReceived(string data)
    {
        try
        {
            var donation = JsonSerializer.Deserialize<DonationAlertsDonation>(data);
            DonationReceived?.Invoke(this, new DonationAlertsDonationEventArgs { Donation = donation });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка парсинга доната");
            OnError($"Ошибка парсинга доната: {ex.Message}", ex);
        }
    }

    private void OnConnected() => Connected?.Invoke(this, EventArgs.Empty);

    private void OnDisconnected(string reason) =>
        Disconnected?.Invoke(this, new DisconnectEventArgs { Reason = reason });

    private void OnError(string message, Exception? exception = null) =>
        Error?.Invoke(this, new ErrorEventArgs { Message = message, Exception = exception });
}
