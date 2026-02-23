using UkiChat.Entities;

namespace UkiChat.Model.VkVideoLive;

public record VkVideoLiveConnectionParams(
    string OldChannelName,
    string ChannelName,
    long ChannelId,
    string WsAccessToken)
{
    public static VkVideoLiveConnectionParams OfVkVideoLiveSettings(string oldChannelName,
        string newChannelName,
        VkVideoLiveSettings vkVideoLiveSettings)
    {
        return new VkVideoLiveConnectionParams(
            oldChannelName, newChannelName, vkVideoLiveSettings.ChannelId ?? 0, vkVideoLiveSettings.WsAccessToken ?? ""
        );
    }
}