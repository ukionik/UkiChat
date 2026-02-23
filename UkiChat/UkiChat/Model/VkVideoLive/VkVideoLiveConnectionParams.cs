namespace UkiChat.Model.VkVideoLive;

public record VkVideoLiveConnectionParams(
    string OldChannelName = "",
    string ChannelName = "",
    long ChannelId = 0,
    string WsAccessToken = "");
