using UkiChat.Model.YouTube;

namespace UkiChat.Model.Chat.EventArgs;

public class YouTubeChatMessageEventArgs : System.EventArgs
{
    public YouTubeChatMessage? Message { get; init; }
    public string VideoId { get; init; } = string.Empty;
}
