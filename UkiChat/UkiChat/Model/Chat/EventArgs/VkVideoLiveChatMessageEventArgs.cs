using UkiChat.Model.VkVideoLive;

namespace UkiChat.Model.Chat.EventArgs;

public class VkVideoLiveChatMessageEventArgs : System.EventArgs
{
    public VkVideoLiveChatMessage? Message { get; init; }
    public long ChannelId { get; init; }
}