using System;
using System.Threading.Tasks;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;

namespace UkiChat.Twitch;

/// <summary>
///     Обёртка над транспортом TwitchLib: пропускает каждую входящую сырую IRC-строку через
///     <see cref="TwitchEmoteIndexNormalizer" /> до того, как её разберёт TwitchClient. Своего хука
///     для правки сырой строки библиотека не даёт, а разбор тега emotes= падает на сообщениях с
///     комбинирующими символами и убивает клиент целиком (см. комментарий в нормализаторе).
///     Остальные события и вызовы просто пробрасываются во внутренний клиент.
/// </summary>
public sealed class EmoteIndexNormalizingClient : IClient
{
    private readonly IClient _inner;

    public EmoteIndexNormalizingClient(IClient inner)
    {
        _inner = inner;

        _inner.OnMessage += async (_, e) =>
        {
            var handler = OnMessage;
            if (handler != null)
                await handler(this, new OnMessageEventArgs(TwitchEmoteIndexNormalizer.Normalize(e.Message)));
        };

        _inner.OnConnected += (_, e) => OnConnected?.Invoke(this, e) ?? Task.CompletedTask;
        _inner.OnDisconnected += (_, e) => OnDisconnected?.Invoke(this, e) ?? Task.CompletedTask;
        _inner.OnError += (_, e) => OnError?.Invoke(this, e) ?? Task.CompletedTask;
        _inner.OnFatality += (_, e) => OnFatality?.Invoke(this, e) ?? Task.CompletedTask;
        _inner.OnSendFailed += (_, e) => OnSendFailed?.Invoke(this, e) ?? Task.CompletedTask;
        _inner.OnReconnected += (_, e) => OnReconnected?.Invoke(this, e) ?? Task.CompletedTask;
    }

    public event AsyncEventHandler<OnConnectedEventArgs>? OnConnected;
    public event AsyncEventHandler<OnDisconnectedEventArgs>? OnDisconnected;
    public event AsyncEventHandler<OnErrorEventArgs>? OnError;
    public event AsyncEventHandler<OnFatalErrorEventArgs>? OnFatality;
    public event AsyncEventHandler<OnMessageEventArgs>? OnMessage;
    public event AsyncEventHandler<OnSendFailedEventArgs>? OnSendFailed;
    public event AsyncEventHandler<OnConnectedEventArgs>? OnReconnected;

    public bool IsConnected => _inner.IsConnected;

    public IClientOptions Options => _inner.Options;

    public Task<bool> OpenAsync() => _inner.OpenAsync();

    public Task<bool> ReconnectAsync() => _inner.ReconnectAsync();

    public Task CloseAsync() => _inner.CloseAsync();

    public Task<bool> SendAsync(string message) => _inner.SendAsync(message);

    public void Dispose() => _inner.Dispose();
}
