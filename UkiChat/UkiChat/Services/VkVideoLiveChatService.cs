using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UkiChat.Model.VkVideoLive;

namespace UkiChat.Services;

/// <summary>
/// Сервис для работы с чатом VK Video Live через WebSocket с Protobuf протоколом
/// </summary>
public class VkVideoLiveChatService : IVkVideoLiveChatService, IDisposable
{
    private const string WebSocketUrl = "wss://pubsub.live.vkvideo.ru/connection/websocket?cf_protocol_version=v2";

    private readonly IVkVideoLiveApiService _apiService;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private string? _chatChannel;
    private string? _clientId;
    private uint _commandId;
    private bool _disposed;
    private bool _isConnected;

    public event EventHandler<ChatMessageEventArgs>? MessageReceived;
    public event EventHandler? Connected;
    public event EventHandler<DisconnectEventArgs>? Disconnected;
    public event EventHandler<ErrorEventArgs>? Error;

    public VkVideoLiveChatService(IVkVideoLiveApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task ConnectAsync(string accessToken, string chatChannel)
    {
        try
        {
            // Убираем префикс "api-" из названия канала если он есть
            _chatChannel = chatChannel.Replace("api-channel-", "channel-");
            _commandId = 0;
            _isConnected = false;

            Console.WriteLine($"[VkVideoLiveChat] Используем канал: {_chatChannel} (оригинал: {chatChannel})");

            // Получаем WebSocket токен через API
            Console.WriteLine("[VkVideoLiveChat] Получение WebSocket токена...");
            var wsTokenResponse = await _apiService.GetWebSocketTokenAsync(accessToken);
            var wsToken = wsTokenResponse.Data.Token;
            Console.WriteLine($"[VkVideoLiveChat] WebSocket токен получен: {wsToken.Substring(0, Math.Min(10, wsToken.Length))}...");

            // Создаем WebSocket клиент
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            // Добавляем заголовки
            _webSocket.Options.SetRequestHeader("Origin", "https://vkvideo.ru");
            _webSocket.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            // Подключаемся к WebSocket серверу
            Console.WriteLine($"[VkVideoLiveChat] Подключение к WebSocket: {WebSocketUrl}");
            await _webSocket.ConnectAsync(new Uri(WebSocketUrl), _cancellationTokenSource.Token);
            Console.WriteLine("[VkVideoLiveChat] WebSocket подключен");

            // Запускаем цикл получения сообщений
            _ = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            // Отправляем команду подключения с WebSocket токеном
            await SendConnectCommandAsync(wsToken);

            // Даем время на обработку подключения
            await Task.Delay(1000);

            // Отправляем команду подписки на канал
            await SendSubscribeCommandAsync(_chatChannel);

            // Даем время на обработку подписки
            await Task.Delay(500);
        }
        catch (Exception ex)
        {
            OnError($"Ошибка подключения к чату: {ex.Message}", ex);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _isConnected = false;

            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect requested", CancellationToken.None);
            }

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
    /// Отправляет команду подключения к Centrifuge серверу (JSON)
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
    /// Отправляет команду подписки на канал (JSON)
    /// </summary>
    private async Task SendSubscribeCommandAsync(string channel)
    {
        var command = new
        {
            subscribe = new
            {
                channel = channel
            },
            id = ++_commandId
        };

        var json = JsonSerializer.Serialize(command);
        Console.WriteLine($"[VkVideoLiveChat] Отправка subscribe command: {json}");
        await SendJsonCommandAsync(json);
    }

    /// <summary>
    /// Отправляет команду в WebSocket (JSON text)
    /// </summary>
    private async Task SendJsonCommandAsync(string json)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException("WebSocket не подключен");
        }

        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        Console.WriteLine($"[VkVideoLiveChat] Отправка {bytes.Length} байт (JSON)");
        await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationTokenSource?.Token ?? CancellationToken.None);
    }

    /// <summary>
    /// Отправляет pong ответ на ping от сервера
    /// </summary>
    private async Task SendPongAsync()
    {
        try
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                return;
            }

            // В Centrifuge v2 протоколе pong - это пустой JSON объект
            var pongJson = "{}";
            var bytes = Encoding.UTF8.GetBytes(pongJson);
            var buffer = new ArraySegment<byte>(bytes);

            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, _cancellationTokenSource?.Token ?? CancellationToken.None);
            Console.WriteLine("[VkVideoLiveChat] Pong отправлен");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VkVideoLiveChat] Ошибка отправки pong: {ex.Message}");
        }
    }

    /// <summary>
    /// Цикл получения сообщений из WebSocket
    /// </summary>
    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (_webSocket != null && _webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine($"[VkVideoLiveChat] Получен Close: {result.CloseStatus} - {result.CloseStatusDescription}");
                    OnDisconnected(result.CloseStatusDescription ?? "Connection closed");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"[VkVideoLiveChat] Получено {result.Count} байт (JSON): {json}");
                    await ProcessJsonMessageAsync(json);
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
    /// Обрабатывает полученное JSON сообщение
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
                OnError($"Ошибка сервера [{code}]: {message}", null);
                return;
            }

            // Обрабатываем ответ на команду connect
            if (root.TryGetProperty("connect", out var connectProp))
            {
                _clientId = connectProp.TryGetProperty("client", out var clientProp) ? clientProp.GetString() : "";

                // Получаем ping интервал из ответа сервера (для информации)
                var pingInterval = connectProp.TryGetProperty("ping", out var pingProp)
                    ? pingProp.GetInt32()
                    : 25;

                Console.WriteLine($"[VkVideoLiveChat] Подключено. Client ID: {_clientId}, Ping interval: {pingInterval}s");
                _isConnected = true;

                OnConnected();
            }

            // Обрабатываем ответ на команду subscribe
            if (root.TryGetProperty("subscribe", out _))
            {
                Console.WriteLine("[VkVideoLiveChat] Подписка успешна");
            }

            // Обрабатываем push-сообщения
            if (root.TryGetProperty("push", out var pushProp))
            {
                ProcessPushMessage(pushProp);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            OnError($"Ошибка обработки сообщения: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Обрабатывает push-сообщение от сервера
    /// </summary>
    private void ProcessPushMessage(JsonElement push)
    {
        try
        {
            var channel = push.TryGetProperty("channel", out var channelProp) ? channelProp.GetString() : _chatChannel ?? string.Empty;

            // Обрабатываем публикацию (сообщение в чате)
            if (push.TryGetProperty("pub", out var pubProp))
            {
                var data = pubProp.TryGetProperty("data", out var dataProp) ? dataProp.GetRawText() : "{}";
                Console.WriteLine($"[VkVideoLiveChat] Получено сообщение из канала '{channel}': {data}");
                OnMessageReceived(data, channel);
            }

            // Обрабатываем join/leave события если нужно
            if (push.TryGetProperty("join", out var joinProp))
            {
                Console.WriteLine($"[VkVideoLiveChat] Join в канале '{channel}'");
            }

            if (push.TryGetProperty("leave", out var leaveProp))
            {
                Console.WriteLine($"[VkVideoLiveChat] Leave из канала '{channel}'");
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

    private void OnConnected()
    {
        Connected?.Invoke(this, EventArgs.Empty);
    }

    private void OnDisconnected(string reason)
    {
        _isConnected = false;
        Disconnected?.Invoke(this, new DisconnectEventArgs { Reason = reason });
    }

    private void OnMessageReceived(string data, string channel)
    {
        try
        {
            var message = JsonSerializer.Deserialize<VkVideoLiveChatMessage>(data);
            MessageReceived?.Invoke(this, new ChatMessageEventArgs
            {
                Message = message,
                Channel = channel ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VkVideoLiveChat] Ошибка парсинга сообщения: {ex.Message}");
            OnError($"Ошибка парсинга сообщения: {ex.Message}", ex);
        }
    }

    private void OnError(string message, Exception? exception = null)
    {
        Error?.Invoke(this, new ErrorEventArgs
        {
            Message = message,
            Exception = exception
        });
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
}
