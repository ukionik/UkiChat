namespace UkiChat.Model.Chat.EventArgs;

public class DisconnectEventArgs : System.EventArgs
{
    public string Reason { get; init; } = string.Empty;
    public string ChannelName { get; init; } = string.Empty;

    /// <summary>Стоит ли пытаться переподключиться. Сервер может прислать reconnect:false.</summary>
    public bool Reconnect { get; init; } = true;
}