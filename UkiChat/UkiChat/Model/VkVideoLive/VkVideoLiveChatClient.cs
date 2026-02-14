using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UkiChat.Services;

namespace UkiChat.Model.VkVideoLive;

public class VkVideoLiveChatClient : IDisposable
{
    private const string WebSocketUrl = "wss://pubsub.live.vkvideo.ru/connection/websocket?cf_protocol_version=v2";
    private CancellationTokenSource? _cancellationTokenSource;
    private int _commandId;
    private bool _disposed;
    private ClientWebSocket? _webSocket;
    private string _channel = "";

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
    public event EventHandler<ErrorEventArgs>? Error;

    public async Task ConnectAsync(string wsToken, string channel)
    {
        try
        {
            _channel = channel;
            _commandId = 0;
            _cancellationTokenSource = new CancellationTokenSource();
            Console.WriteLine("[VkVideoLiveChat] Подключение к WebSocket...");
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cancellationTokenSource.Token);
            Console.WriteLine("[VkVideoLiveChat] WebSocket подключен");
            // Подключаемся к WebSocket серверу
            // Запускаем цикл получения сообщений
            _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            // Отправляем команду подключения с WebSocket токеном
            await SendConnectCommandAsync(wsToken);

            // Отправляем команду подписки на канал
            await SendSubscribeCommandAsync(channel);
        }
        catch (Exception e)
        {
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

            Console.WriteLine("[VkVideoLiveChat] Отключено");
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
        var messageBuilder = new StringBuilder();

        try
        {
            while (_webSocket is { State: WebSocketState.Open } && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine(
                        $"[VkVideoLiveChat] Получен Close: {result.CloseStatus} - {result.CloseStatusDescription}");
                    OnDisconnected(result.CloseStatusDescription ?? "Connection closed");
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text) 
                    continue;
                
                // Накапливаем фрагменты сообщения
                var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
                messageBuilder.Append(chunk);

                // Обрабатываем только когда получено полное сообщение
                if (result.EndOfMessage)
                {
                    var json = messageBuilder.ToString();
                    messageBuilder.Clear();

                    Console.WriteLine($"[VkVideoLiveChat] Получено полное сообщение ({json.Length} символов)");
                    await ProcessJsonMessageAsync(json);
                }
                else
                {
                    Console.WriteLine(
                        $"[VkVideoLiveChat] Получен фрагмент {result.Count} байт, накоплено {messageBuilder.Length} символов");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[VkVideoLiveChat] Получение сообщений отменено");
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"[VkVideoLiveChat] WebSocket ошибка: {ex.Message}");
            OnError($"WebSocket ошибка: {ex.Message}", ex);
            OnDisconnected($"WebSocket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VkVideoLiveChat] Ошибка получения сообщений: {ex.Message}");
            OnError($"Ошибка получения сообщений: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Обрабатывает полученное JSON сообщение
    /// </summary>
    private async Task ProcessJsonMessageAsync(string json)
    {
        try
        {
            // Проверяем на ping от сервера (пустой JSON объект)
            if (json.Trim() == "{}")
            {
                Console.WriteLine("[VkVideoLiveChat] Получен ping от сервера, отправляем pong");
                await SendPongAsync();
                return;
            }

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            // Проверяем на пустой объект (ping)
            if (root.ValueKind == JsonValueKind.Object && root.EnumerateObject().MoveNext() == false)
            {
                Console.WriteLine("[VkVideoLiveChat] Получен ping (пустой объект), отправляем pong");
                await SendPongAsync();
                return;
            }

            // Получаем ID сообщения (0 для push от сервера)
            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetUInt32() : 0;
            Console.WriteLine($"[VkVideoLiveChat] Получен Reply (ID: {id})");

            // Обрабатываем ошибку если есть
            if (root.TryGetProperty("error", out var errorProp))
            {
                var code = errorProp.TryGetProperty("code", out var codeProp) ? codeProp.GetUInt32() : 0;
                var message = errorProp.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "";
                Console.WriteLine($"[VkVideoLiveChat] Ошибка: [{code}] {message}");
                OnError($"Ошибка сервера [{code}]: {message}");
                return;
            }

            // Обрабатываем ответ на команду connect
            if (root.TryGetProperty("connect", out var connectProp))
            {
                // Получаем ping интервал из ответа сервера (для информации)
                var pingInterval = connectProp.TryGetProperty("ping", out var pingProp)
                    ? pingProp.GetInt32()
                    : 25;

                Console.WriteLine($"[VkVideoLiveChat] Подключено. Ping interval: {pingInterval}s");
                OnConnected();
            }

            // Обрабатываем ответ на команду subscribe
            if (root.TryGetProperty("subscribe", out _))
                Console.WriteLine("[VkVideoLiveChat] Подписка успешна");

            // Обрабатываем push-сообщения
            if (root.TryGetProperty("push", out var pushProp)) ProcessPushMessage(pushProp);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            OnError($"Ошибка обработки сообщения: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Обрабатывает push-сообщение от сервера
    /// </summary>
    private void ProcessPushMessage(JsonElement push)
    {
        try
        {
            Console.WriteLine(push.GetRawText());

            // Обрабатываем публикацию (сообщение в чате)
            if (push.TryGetProperty("pub", out var pubProp))
            {
                var data = pubProp.TryGetProperty("data", out var dataProp) ? dataProp.GetRawText() : "{}";
                Console.WriteLine($"[VkVideoLiveChat] Получено сообщение из канала '{_channel}': {data}");
                OnMessageReceived(data, _channel);
            }

            // Обрабатываем join/leave события если нужно
            if (push.TryGetProperty("join", out _))
            {
                Console.WriteLine($"[VkVideoLiveChat] Join в канале '{_channel}'");
            }

            if (push.TryGetProperty("leave", out _))
            {
                Console.WriteLine($"[VkVideoLiveChat] Leave из канала '{_channel}'");
            }

            // Обрабатываем отписку
            if (push.TryGetProperty("unsubscribe", out var unsubProp))
            {
                var code = unsubProp.TryGetProperty("code", out var codeProp) ? codeProp.GetUInt32() : 0;
                var reason = unsubProp.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "";
                Console.WriteLine($"[VkVideoLiveChat] Unsubscribe: [{code}] {reason}");
            }

            // Обрабатываем disconnect
            if (push.TryGetProperty("disconnect", out var disconnectProp))
            {
                var code = disconnectProp.TryGetProperty("code", out var codeProp) ? codeProp.GetUInt32() : 0;
                var reason = disconnectProp.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "";
                Console.WriteLine($"[VkVideoLiveChat] Disconnect push: [{code}] {reason}");
                OnDisconnected(reason ?? "");
            }
        }
        catch (Exception ex)
        {
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
        Console.WriteLine($"[VkVideoLiveChat] Отправка connect command: {json}");
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
        Console.WriteLine($"[VkVideoLiveChat] Отправка subscribe command: {json}");
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

        Console.WriteLine($"[VkVideoLiveChat] Отправка {bytes.Length} байт (JSON)");
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
            Console.WriteLine("[VkVideoLiveChat] Pong отправлен");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VkVideoLiveChat] Ошибка отправки pong: {ex.Message}");
        }
    }
    
    private void OnMessageReceived(string data, string channel)
    {
        try
        {
            var message = JsonSerializer.Deserialize<VkVideoLiveChatMessage>(data);
            MessageReceived?.Invoke(this, new ChatMessageEventArgs
            {
                Message = message,
                Channel = channel
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VkVideoLiveChat] Ошибка парсинга сообщения: {ex.Message}");
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
        Error?.Invoke(this, new ErrorEventArgs
        {
            Message = message,
            Exception = exception
        });
    }
}