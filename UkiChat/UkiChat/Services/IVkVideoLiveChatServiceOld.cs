using System;
using System.Threading.Tasks;
using UkiChat.Model.VkVideoLive;

namespace UkiChat.Services;

/// <summary>
/// Сервис для работы с чатом VK Video Live через WebSocket
/// </summary>
public interface IVkVideoLiveChatServiceOld
{
    /// <summary>
    /// Подключиться к чату канала
    /// </summary>
    /// <param name="accessToken">Токен доступа</param>
    /// <param name="chatChannel">Канал чата (например, api-channel-chat:7511387)</param>
    Task ConnectAsync(string accessToken, string chatChannel);

    /// <summary>
    /// Отключиться от чата
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Событие получения нового сообщения
    /// </summary>
    event EventHandler<ChatMessageEventArgs>? MessageReceived;

    /// <summary>
    /// Событие подключения
    /// </summary>
    event EventHandler? Connected;

    /// <summary>
    /// Событие отключения
    /// </summary>
    event EventHandler<DisconnectEventArgs>? Disconnected;

    /// <summary>
    /// Событие ошибки
    /// </summary>
    event EventHandler<ErrorEventArgs>? Error;
}

/// <summary>
/// Аргументы события получения сообщения чата
/// </summary>
public class ChatMessageEventArgs : EventArgs
{
    public VkVideoLiveChatMessage? Message { get; init; }
    public string Channel { get; init; } = string.Empty;
}

/// <summary>
/// Аргументы события отключения
/// </summary>
public class DisconnectEventArgs : EventArgs
{
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Аргументы события ошибки
/// </summary>
public class ErrorEventArgs : EventArgs
{
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
}
