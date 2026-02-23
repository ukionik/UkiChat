using UkiChat.Entities;

namespace UkiChat.Model.Twitch;

public record TwitchConnectionParams(
    string OldChannel,
    string NewChannel,
    string BroadcasterId,
    string ChatbotUsername,
    string ChatbotAccessToken
)
{
    public static TwitchConnectionParams OfTwitchSettings(string oldChannel,
        string newChannel,
        TwitchSettings twitchSettings)
    {
        return new TwitchConnectionParams(
            oldChannel,
            newChannel,
            twitchSettings.ApiBroadcasterId ?? "",
            twitchSettings.ChatbotUsername ?? "",
            twitchSettings.ChatbotAccessToken ?? ""
        );
    }
}