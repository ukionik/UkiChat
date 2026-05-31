using UkiChat.Entities;

namespace UkiChat.Model.YouTube;

public record YouTubeConnectionParams(
    string OldChannelName,
    string ChannelName)
{
    public static YouTubeConnectionParams OfYouTubeSettings(string oldChannelName,
        string newChannelName,
        YouTubeSettings youTubeSettings)
    {
        return new YouTubeConnectionParams(oldChannelName, newChannelName);
    }
}
