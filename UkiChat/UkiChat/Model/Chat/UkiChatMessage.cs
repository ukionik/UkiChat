using TwitchLib.Client.Models;

namespace UkiChat.Model.Chat;

public record UkiChatMessage(string DisplayName, string Message)
{
    public static UkiChatMessage FromTwitchMessage(ChatMessage twitchMessage)
    {
        return new UkiChatMessage(twitchMessage.DisplayName, twitchMessage.Message);
    }

    public static UkiChatMessage FromTwitchMessageNotification(string message)
    {
        return new UkiChatMessage("Twitch", message);
    }
};