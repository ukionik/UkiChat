using System.Collections.Generic;
using TwitchLib.Client.Models;

namespace UkiChat.Model.Chat;

public record UkiChatMessage(ChatPlatform Platform
    , List<string> Badges
    , string DisplayName
    , string Message)
{
    public static UkiChatMessage FromTwitchMessage(ChatMessage twitchMessage, List<string> badgeUrls)
    {
        return new UkiChatMessage( ChatPlatform.Twitch, badgeUrls, twitchMessage.DisplayName, twitchMessage.Message);
    }

    public static UkiChatMessage FromTwitchMessageNotification(string message)
    {
        return new UkiChatMessage(ChatPlatform.Twitch, [], ChatPlatform.Twitch.ToString(), message);
    }
};