using System.Collections.Generic;
using System.Linq;
using TwitchLib.Client.Models;
using UkiChat.Utils;

namespace UkiChat.Model.Chat;

public record UkiChatMessage(ChatPlatform Platform
    , List<string> Badges
    , string DisplayName
    , string DisplayNameColor
    , List<UkiChatMessagePart> MessageParts)
{
    public static UkiChatMessage FromTwitchMessage(ChatMessage twitchMessage, List<string> badgeUrls)
    {
        var messageParts = ParseMessageParts(twitchMessage.Message, twitchMessage.EmoteSet);
        var displayNameColor = ColorUtil.GetDisplayNameColor(twitchMessage.DisplayName, twitchMessage.HexColor);
        return new UkiChatMessage(ChatPlatform.Twitch, badgeUrls, twitchMessage.DisplayName, displayNameColor, messageParts);
    }

    public static UkiChatMessage FromTwitchMessageNotification(string message)
    {
        return new UkiChatMessage(ChatPlatform.Twitch, [], ChatPlatform.Twitch.ToString(), "#FFFFFF",
            [new UkiChatMessagePart(UkiChatMessagePartType.Text, message)]);
    }

    private static List<UkiChatMessagePart> ParseMessageParts(string message, EmoteSet emoteSet)
    {
        var parts = new List<UkiChatMessagePart>();

        if (emoteSet.Emotes.Count == 0)
        {
            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, message));
            return parts;
        }

        // Сортируем эмоуты по позиции начала
        var sortedEmotes = emoteSet.Emotes.OrderBy(e => e.StartIndex).ToList();
        var currentIndex = 0;

        foreach (var emote in sortedEmotes)
        {
            // Добавляем текст перед эмоутом (если есть)
            if (emote.StartIndex > currentIndex)
            {
                var textBefore = message.Substring(currentIndex, emote.StartIndex - currentIndex);
                parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, textBefore));
            }

            // Добавляем эмоут (URL изображения)
            var emoteUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/3.0";
            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Emote, emoteUrl));

            currentIndex = emote.EndIndex + 1;
        }

        // Добавляем оставшийся текст после последнего эмоута
        if (currentIndex < message.Length)
        {
            var textAfter = message[currentIndex..];
            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, textAfter));
        }

        return parts;
    }
};