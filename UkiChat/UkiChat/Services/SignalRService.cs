using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using UkiChat.Diagnostics;
using UkiChat.Hubs;
using UkiChat.Model.Chat;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

/// <summary>
///     Рассылка событий во фронтенд. Отправка НАМЕРЕННО развязана с вызывающим потоком: методы
///     только кладут сообщение в очередь и сразу возвращаются.
///     Это критично, потому что рассылку вызывают обработчики событий TwitchLib, а библиотека
///     await-ит их прямо внутри цикла чтения WebSocket. Раньше здесь был Clients.All.SendAsync,
///     который ждёт записи ВО ВСЕ подключения: одного зависшего клиента (например, вкладки OBS,
///     переставшей забирать long-polling, — её буфер переполняется, и запись встаёт навсегда)
///     хватало, чтобы намертво остановить чтение чата Twitch. Сторож простоя видел тишину и
///     переподключался, но новый клиент вставал на том же месте — на первом же уведомлении
///     после входа в канал.
///     Поэтому у каждого подключения своя ограниченная очередь и свой насос: зависший клиент
///     теряет свои сообщения (DropOldest), но не задерживает ни других клиентов, ни чат.
/// </summary>
public class SignalRService(IHubContext<AppHub> hubContext) : ISignalRService
{
    // Глубина очереди на клиента. При заходе в канал прилетает история, поэтому запас нужен
    // ощутимый, но переполнение не страшно — теряются самые старые сообщения.
    private const int ClientQueueCapacity = 500;

    // Отправка дольше этого времени означает, что клиент перестал разбирать свою очередь.
    private static readonly TimeSpan SlowSendThreshold = TimeSpan.FromSeconds(5);

    // Чаты поднимаются, не дожидаясь фронтенда (см. AppInitializationService), поэтому первые
    // события — уведомления о подключении к каналам, а то и первые сообщения — успевают уйти
    // в пустоту, пока фронт грузится. Поэтому стартовые события копятся и достаются каждому,
    // кто подключится в этом окне. Окно намеренно короткое и одноразовое: переподключившийся
    // через полчаса OBS не должен получать пачку старья.
    private static readonly TimeSpan StartupBufferWindow = TimeSpan.FromSeconds(60);
    private const int StartupBufferCapacity = 200;

    // Защищает список клиентов и стартовый буфер разом: только так порядок событий у клиента,
    // подключившегося ровно в момент рассылки, остаётся строго последовательным.
    private readonly Lock _sync = new();
    private readonly Dictionary<string, ClientQueue> _clients = new();

    private readonly Stopwatch _sinceStart = Stopwatch.StartNew();
    private List<Envelope>? _startupBuffer = [];

    public void RegisterConnection(string connectionId)
    {
        var queue = new ClientQueue(hubContext, connectionId);
        lock (_sync)
        {
            var buffer = GetLiveStartupBuffer();
            if (buffer is { Count: > 0 })
            {
                foreach (var envelope in buffer)
                    queue.Enqueue(envelope);
                StartupDiagnostics.Log("signalr",
                    $"Клиенту {connectionId} отдано {buffer.Count} стартовых событий");
            }

            _clients[connectionId] = queue;
        }
    }

    public void UnregisterConnection(string connectionId)
    {
        ClientQueue? queue;
        lock (_sync)
        {
            _clients.Remove(connectionId, out queue);
        }

        queue?.Complete();
    }

    // Отдаёт стартовый буфер, пока не истекло окно; после — освобождает его навсегда.
    // Вызывается только под _sync.
    private List<Envelope>? GetLiveStartupBuffer()
    {
        if (_startupBuffer != null && _sinceStart.Elapsed > StartupBufferWindow)
            _startupBuffer = null;
        return _startupBuffer;
    }

    public Task SendChatMessageAsync(UkiChatMessage message) => Broadcast("OnChatMessage", message);

    public Task SendMessageDeletedAsync(string messageId) => Broadcast("OnMessageDeleted", messageId);

    public Task SendUserMessagesDeletedAsync(string username) => Broadcast("OnUserMessagesDeleted", username);

    public Task SendTwitchReconnect() => Broadcast("OnTwitchReconnect");

    public Task SendVkVideoLiveReconnect() => Broadcast("OnVkVideoLiveReconnect");

    public Task SendYouTubeReconnect() => Broadcast("OnYouTubeReconnect");

    public Task SendTwitchAuthChanged(TwitchAuthStatusData status) => Broadcast("OnTwitchAuthChanged", status);

    public Task SendDonationAlertsAuthChanged(DonationAlertsAuthStatusData status) =>
        Broadcast("OnDonationAlertsAuthChanged", status);

    // Кладёт событие в очередь каждого подключения и сразу возвращается — блокировки здесь быть
    // не может по построению.
    private Task Broadcast(string method, params object?[] args)
    {
        var envelope = new Envelope(method, args);
        lock (_sync)
        {
            var buffer = GetLiveStartupBuffer();
            if (buffer != null && buffer.Count < StartupBufferCapacity)
                buffer.Add(envelope);

            foreach (var queue in _clients.Values)
                queue.Enqueue(envelope);
        }

        return Task.CompletedTask;
    }

    private sealed record Envelope(string Method, object?[] Args);

    /// <summary>
    ///     Очередь одного подключения с собственным насосом. Порядок сообщений в рамках клиента
    ///     сохраняется, а зависшая отправка тормозит только этот насос: очередь тем временем
    ///     вытесняет самые старые сообщения, поэтому память ограничена. Если клиент оживёт,
    ///     насос сам продолжит работу с того, что осталось в очереди.
    /// </summary>
    private sealed class ClientQueue
    {
        private readonly Channel<Envelope> _channel = Channel.CreateBounded<Envelope>(
            new BoundedChannelOptions(ClientQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            });

        private readonly string _connectionId;
        private readonly IHubContext<AppHub> _hubContext;

        public ClientQueue(IHubContext<AppHub> hubContext, string connectionId)
        {
            _hubContext = hubContext;
            _connectionId = connectionId;
            _ = Task.Run(PumpAsync);
        }

        public void Enqueue(Envelope envelope) => _channel.Writer.TryWrite(envelope);

        public void Complete() => _channel.Writer.TryComplete();

        private async Task PumpAsync()
        {
            try
            {
                await foreach (var envelope in _channel.Reader.ReadAllAsync())
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        await _hubContext.Clients.Client(_connectionId)
                            .SendCoreAsync(envelope.Method, envelope.Args);
                    }
                    catch (Exception ex)
                    {
                        StartupDiagnostics.LogError("signalr",
                            $"Отправка {envelope.Method} клиенту {_connectionId} не удалась", ex);
                        continue;
                    }

                    if (sw.Elapsed >= SlowSendThreshold)
                        StartupDiagnostics.Log("signalr",
                            $"Медленный клиент {_connectionId}: {envelope.Method} ушёл за {sw.ElapsedMilliseconds} мс");
                }
            }
            catch (Exception ex)
            {
                StartupDiagnostics.LogError("signalr", $"Насос очереди клиента {_connectionId} остановлен", ex);
            }
        }
    }
}
