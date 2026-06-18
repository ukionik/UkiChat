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
///     Клиент Centrifugo (v1 JSON-протокол, сервер 2.2.1) для DonationAlerts.
///     Канал приватный ($alerts:donation_{userId}): после connect сервер выдаёт client id,
///     под который REST-запросом получается per-channel токен подписки.
///     Команды v1: connect — {"params":{"token":..},"id":N}; subscribe — {"params":{"channel":..,"token":..},"method":1,"id":N}.
/// </summary>
public class DonationAlertsCentrifugeClient : IDisposable
{
    private const string WebSocketUrl = "wss://centrifugo.donationalerts.com/connection/websocket";

    // Методы протокола Centrifuge v1.
    private const int MethodSubscribe = 1;
    private const int MethodPing = 7;

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
            // Тихо закрываем предыдущее соединение (без события Disconnected — это не потеря связи).
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
            // Неожиданный обрыв цикла приёма — сигналим о разрыве, чтобы сработал реконнект.
            OnDisconnected($"Receive loop error: {ex.Message}");
        }
    }

    /// <summary>
    ///     Сервер может прислать несколько JSON-объектов в одном фрейме (конкатенация / перевод строки).
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
            // Пустой объект — серверный ping, отвечаем pong.
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

            if (!root.TryGetProperty("result", out var result))
                return;

            // Ответ на connect: result.client → инициируем подписку на приватный канал.
            if (result.TryGetProperty("client", out var clientProp))
            {
                var clientId = clientProp.GetString() ?? "";
                _logger.LogInformation("Подключено к Centrifugo, client={Client}", clientId);
                OnConnected();
                await SubscribeToChannelAsync(clientId);
                return;
            }

            // Серверный disconnect-уведомление.
            if (result.TryGetProperty("type", out var typeProp) && typeProp.TryGetInt32(out var typeVal) && typeVal == 6)
            {
                HandleDisconnectMessage(result);
                return;
            }

            // Публикация (донат): result.data.data — полезная нагрузка.
            if (result.TryGetProperty("data", out var dataProp))
            {
                if (dataProp.TryGetProperty("data", out var donationProp))
                {
                    _logger.LogDebug("Получен донат: {Data}", donationProp.GetRawText());
                    OnDonationReceived(donationProp.GetRawText());
                }
                else
                {
                    // Подтверждение подписки / join — payload без вложенного data.
                    _logger.LogInformation("Подписка/служебное сообщение по каналу {Channel}", _channel);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки JSON-объекта");
            OnError($"Ошибка обработки JSON-объекта: {ex.Message}", ex);
        }
    }

    private void HandleDisconnectMessage(JsonElement result)
    {
        var reconnect = true;
        var reason = "";
        if (result.TryGetProperty("data", out var data))
        {
            if (data.TryGetProperty("reconnect", out var reconnectProp) &&
                reconnectProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                reconnect = reconnectProp.GetBoolean();
            if (data.TryGetProperty("reason", out var reasonProp))
                reason = reasonProp.GetString() ?? "";
        }
        _logger.LogWarning("Серверный disconnect: {Reason} (reconnect={Reconnect})", reason, reconnect);
        OnDisconnected(reason, reconnect);
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

    private async Task SendConnectCommandAsync(string socketConnectionToken)
    {
        // Centrifuge v1: connect — это method 0 (поле method опускается).
        var command = new
        {
            @params = new { token = socketConnectionToken },
            id = ++_commandId
        };

        await SendJsonCommandAsync(JsonSerializer.Serialize(command));
    }

    private async Task SendSubscribeCommandAsync(string channel, string token)
    {
        var command = new
        {
            @params = new { channel, token },
            method = MethodSubscribe,
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

    private void OnDisconnected(string reason, bool reconnect = true) =>
        Disconnected?.Invoke(this, new DisconnectEventArgs { Reason = reason, Reconnect = reconnect });

    private void OnError(string message, Exception? exception = null) =>
        Error?.Invoke(this, new ErrorEventArgs { Message = message, Exception = exception });
}
