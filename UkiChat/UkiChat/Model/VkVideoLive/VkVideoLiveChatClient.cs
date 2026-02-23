using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UkiChat.Services;

namespace UkiChat.Model.VkVideoLive;

public class VkVideoLiveChatClient : IDisposable
{
    private const string WebSocketUrl = "wss://pubsub.live.vkvideo.ru/connection/websocket?cf_protocol_version=v2";
    private CancellationTokenSource? _cancellationTokenSource;
    private int _commandId;
    private bool _disposed;
    private ClientWebSocket? _webSocket;
    private long _channelId;
    private readonly ILogger<VkVideoLiveChatClient> _logger;

    public VkVideoLiveChatClient(ILogger<VkVideoLiveChatClient> logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _webSocket?.Dispose();

        _disposed = true;
    }

    public event EventHandler<ChatMessageEventArgs>? MessageReceived;
    public event EventHandler? Connected;
    public event EventHandler<DisconnectEventArgs>? Disconnected;
    public event EventHandler<Services.ErrorEventArgs>? Error;

    public async Task ConnectAsync(string wsToken, long channelId)
    {
        try
        {
            _channelId = channelId;
            var channel = $"channel-chat:{_channelId}";
            _commandId = 0;
            _cancellationTokenSource = new CancellationTokenSource();
            _logger.LogInformation("Подключение к WebSocket...");
            // Подключаемся к WebSocket серверу
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Origin", "https://vkvideo.ru");
            await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cancellationTokenSource.Token);
            _logger.LogInformation("WebSocket подключен");
            // Запускаем цикл получения сообщений
            _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            // Отправляем команду подключения с WebSocket токеном
            await SendConnectCommandAsync(wsToken);
            // Отправляем команду подписки на канал
            await SendSubscribeCommandAsync(channel);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Ошибка подключения к чату");
            OnError($"Ошибка подключения к чату: {e.Message}", e);
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
            OnError($"Ошибка отключения от чата: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    ///     Цикл получения сообщений из WebSocket
    /// </summary>
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

                // Накапливаем байты фрагментов — без декодирования, чтобы не разрезать многобайтные символы
                messageStream.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                {
                    var messageBytes = messageStream.ToArray();
                    messageStream.SetLength(0);

                    _logger.LogDebug("Получено полное сообщение ({Length} байт): {Json}",
                        messageBytes.Length, Encoding.UTF8.GetString(messageBytes));
                    await ProcessJsonMessageAsync(messageBytes);
                }
                else
                {
                    _logger.LogDebug("Получен фрагмент {Count} байт, накоплено {Total} байт",
                        result.Count, messageStream.Length);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Получение сообщений отменено");
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
    ///     Разбирает байты сообщения и обрабатывает каждый JSON-объект.
    ///     Сервер может прислать несколько объектов в одном фрейме.
    /// </summary>
    private async Task ProcessJsonMessageAsync(byte[] messageBytes)
    {
        var offset = 0;
        while (offset < messageBytes.Length)
        {
            // Пропускаем пробельные символы между JSON-объектами
            while (offset < messageBytes.Length && messageBytes[offset] <= 32)
                offset++;

            if (offset >= messageBytes.Length) break;

            try
            {
                // Utf8JsonReader — ref struct, нельзя хранить за await.
                // Клонируем JsonElement до await, чтобы он не зависел от документа.
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

    /// <summary>
    ///     Обрабатывает один JSON-объект из сообщения
    /// </summary>
    private async Task ProcessJsonObjectAsync(JsonElement root)
    {
        try
        {
            // Проверяем на пустой объект (ping)
            if (root.ValueKind == JsonValueKind.Object && !root.EnumerateObject().MoveNext())
            {
                _logger.LogDebug("Получен ping (пустой объект), отправляем pong");
                await SendPongAsync();
                return;
            }

            // Получаем ID сообщения (0 для push от сервера)
            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetUInt32() : 0;
            _logger.LogDebug("Получен Reply (ID: {Id})", id);

            // Обрабатываем ошибку если есть
            if (root.TryGetProperty("error", out var errorProp))
            {
                var code = errorProp.TryGetProperty("code", out var codeProp) ? codeProp.GetUInt32() : 0;
                var message = errorProp.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "";
                _logger.LogError("Ошибка сервера [{Code}] {Message}", code, message);
                OnError($"Ошибка сервера [{code}]: {message}");
                return;
            }

            // Обрабатываем ответ на команду connect
            if (root.TryGetProperty("connect", out var connectProp))
            {
                var pingInterval = connectProp.TryGetProperty("ping", out var pingProp)
                    ? pingProp.GetInt32()
                    : 25;
                _logger.LogInformation("Подключено. Ping interval: {PingInterval}s", pingInterval);
                OnConnected();
            }

            // Обрабатываем ответ на команду subscribe
            if (root.TryGetProperty("subscribe", out _))
                _logger.LogInformation("Подписка успешна");

            // Обрабатываем push-сообщения
            if (root.TryGetProperty("push", out var pushProp)) ProcessPushMessage(pushProp);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки JSON-объекта");
            OnError($"Ошибка обработки JSON-объекта: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Обрабатывает push-сообщение от сервера
    /// </summary>
    private void ProcessPushMessage(JsonElement push)
    {
        try
        {
            _logger.LogDebug("Push: {Raw}", push.GetRawText());

            // Обрабатываем публикацию (сообщение в чате)
            if (push.TryGetProperty("pub", out var pubProp))
            {
                var data = pubProp.TryGetProperty("data", out var dataProp) ? dataProp.GetRawText() : "{}";
                _logger.LogDebug("Получено сообщение из канала '{ChannelId}': {Data}", _channelId, data);
                OnMessageReceived(data, _channelId);
            }

            // Обрабатываем join/leave события если нужно
            if (push.TryGetProperty("join", out _))
                _logger.LogDebug("Join в канале '{ChannelId}'", _channelId);

            if (push.TryGetProperty("leave", out _))
                _logger.LogDebug("Leave из канала '{ChannelId}'", _channelId);

            // Обрабатываем отписку
            if (push.TryGetProperty("unsubscribe", out var unsubProp))
            {
                var code = unsubProp.TryGetProperty("code", out var codeProp) ? codeProp.GetUInt32() : 0;
                var reason = unsubProp.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "";
                _logger.LogWarning("Unsubscribe: [{Code}] {Reason}", code, reason);
            }

            // Обрабатываем disconnect
            if (push.TryGetProperty("disconnect", out var disconnectProp))
            {
                var code = disconnectProp.TryGetProperty("code", out var codeProp) ? codeProp.GetUInt32() : 0;
                var reason = disconnectProp.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "";
                _logger.LogWarning("Disconnect push: [{Code}] {Reason}", code, reason);
                OnDisconnected(reason ?? "");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки push-сообщения");
            OnError($"Ошибка обработки push-сообщения: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Отправляет команду подключения к Centrifuge серверу (JSON)
    /// </summary>
    private async Task SendConnectCommandAsync(string wsToken)
    {
        var command = new
        {
            connect = new
            {
                token = wsToken,
                name = "UkiChat"
            },
            id = ++_commandId
        };

        var json = JsonSerializer.Serialize(command);
        _logger.LogDebug("Отправка connect command: {Json}", json);
        await SendJsonCommandAsync(json);
    }

    /// <summary>
    ///     Отправляет команду подписки на канал (JSON)
    /// </summary>
    private async Task SendSubscribeCommandAsync(string channel)
    {
        var command = new
        {
            subscribe = new
            {
                channel
            },
            id = ++_commandId
        };

        var json = JsonSerializer.Serialize(command);
        _logger.LogDebug("Отправка subscribe command: {Json}", json);
        await SendJsonCommandAsync(json);
    }

    /// <summary>
    ///     Отправляет команду в WebSocket (JSON text)
    /// </summary>
    private async Task SendJsonCommandAsync(string json)
    {
        if (_webSocket is not { State: WebSocketState.Open })
            throw new InvalidOperationException("WebSocket не подключен");

        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        _logger.LogDebug("Отправка {Size} байт (JSON)", bytes.Length);
        await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true,
            _cancellationTokenSource?.Token ?? CancellationToken.None);
    }

    /// <summary>
    ///     Отправляет pong ответ на ping от сервера
    /// </summary>
    private async Task SendPongAsync()
    {
        try
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open) return;

            // В Centrifuge v2 протоколе pong - это пустой JSON объект
            var pongJson = "{}";
            var bytes = Encoding.UTF8.GetBytes(pongJson);
            var buffer = new ArraySegment<byte>(bytes);

            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true,
                _cancellationTokenSource?.Token ?? CancellationToken.None);
            _logger.LogDebug("Pong отправлен");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки pong");
        }
    }
    
    private void OnMessageReceived(string data, long channelId)
    {
        try
        {
            var message = JsonSerializer.Deserialize<VkVideoLiveChatMessage>(data);
            MessageReceived?.Invoke(this, new ChatMessageEventArgs
            {
                Message = message,
                ChannelId = channelId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка парсинга сообщения");
            OnError($"Ошибка парсинга сообщения: {ex.Message}", ex);
        }
    }

    private void OnConnected()
    {
        Connected?.Invoke(this, EventArgs.Empty);
    }

    private void OnDisconnected(string reason)
    {
        Disconnected?.Invoke(this, new DisconnectEventArgs { Reason = reason });
    }

    private void OnError(string message, Exception? exception = null)
    {
        Error?.Invoke(this, new Services.ErrorEventArgs
        {
            Message = message,
            Exception = exception
        });
    }
}