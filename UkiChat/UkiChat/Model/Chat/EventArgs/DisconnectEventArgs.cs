namespace UkiChat.Model.Chat.EventArgs;

public class DisconnectEventArgs : System.EventArgs
{
    public string Reason { get; init; } = string.Empty;
}