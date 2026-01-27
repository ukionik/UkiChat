using System.Collections.Generic;
using System.Linq;
using TwitchLib.Client.Models;
using UkiChat.Model.SevenTv;
using UkiChat.Utils;

namespace UkiChat.Model.Chat;

public record UkiChatMessage(ChatPlatform Platform
    , List<string> Badges
    , string DisplayName
    , string DisplayNameColor
    , List<UkiChatMessagePart> MessageParts)
{
    public static UkiChatMessage FromTwitchMessage(ChatMessage twitchMessage, List<string> badgeUrls, Dictionary<string, SevenTvEmote>? sevenTvEmotes = null)
    {
        var messageParts = ParseMessageParts(twitchMessage.Message, twitchMessage.EmoteSet, sevenTvEmotes);
        var displayNameColor = ColorUtil.GetDisplayNameColor(twitchMessage.DisplayName, twitchMessage.HexColor);
        return new UkiChatMessage(ChatPlatform.Twitch, badgeUrls, twitchMessage.DisplayName, displayNameColor, messageParts);
    }

    public static UkiChatMessage FromTwitchMessageNotification(string message)
    {
        return new UkiChatMessage(ChatPlatform.Twitch, [], ChatPlatform.Twitch.ToString(), "#FFFFFF",
            [new UkiChatMessagePart(UkiChatMessagePartType.Text, message)]);
    }

    private static List<UkiChatMessagePart> ParseMessageParts(string message, EmoteSet emoteSet, Dictionary<string, SevenTvEmote>? sevenTvEmotes = null)
    {
        var parts = new List<UkiChatMessagePart>();

        // Если нет ни Twitch, ни 7TV эмоутов, возвращаем текст как есть
        if (emoteSet.Emotes.Count == 0 && (sevenTvEmotes == null || sevenTvEmotes.Count == 0))
        {
            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, message));
            return parts;
        }

        // Сортируем Twitch эмоуты по позиции начала
        var sortedEmotes = emoteSet.Emotes.OrderBy(e => e.StartIndex).ToList();
        var currentIndex = 0;

        foreach (var emote in sortedEmotes)
        {
            // Обрабатываем текст перед Twitch эмоутом (может содержать 7TV эмоуты)
            if (emote.StartIndex > currentIndex)
            {
                var textBefore = message.Substring(currentIndex, emote.StartIndex - currentIndex);
                ParseTextWith7TvEmotes(textBefore, sevenTvEmotes, parts);
            }

            // Добавляем Twitch эмоут
            var emoteUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{emote.Id}/default/dark/3.0";
            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Emote, emoteUrl));

            currentIndex = emote.EndIndex + 1;
        }

        // Обрабатываем оставшийся текст после последнего Twitch эмоута
        if (currentIndex < message.Length)
        {
            var textAfter = message[currentIndex..];
            ParseTextWith7TvEmotes(textAfter, sevenTvEmotes, parts);
        }

        return parts;
    }

    /// <summary>
    /// Парсит текст, заменяя слова на 7TV эмоуты там, где это возможно
    /// </summary>
    private static void ParseTextWith7TvEmotes(string text, Dictionary<string, SevenTvEmote>? sevenTvEmotes, List<UkiChatMessagePart> parts)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // Если нет 7TV эмоутов, добавляем текст как есть
        if (sevenTvEmotes == null || sevenTvEmotes.Count == 0)
        {
            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, text));
            return;
        }

        // Разбиваем текст на слова (по пробелам)
        var words = text.Split(' ');
        var currentText = "";

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];

            // Проверяем, является ли слово 7TV эмоутом
            if (sevenTvEmotes.TryGetValue(word, out var emote))
            {
                // Сохраняем накопленный текст перед эмоутом
                if (!string.IsNullOrEmpty(currentText))
                {
                    parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, currentText));
                    currentText = "";
                }

                // Добавляем 7TV эмоут
                parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Emote, emote.Url));
            }
            else
            {
                // Накапливаем текст
                currentText += word;
            }

            // Добавляем пробел между словами (кроме последнего)
            if (i < words.Length - 1)
            {
                currentText += " ";
            }
        }

        // Добавляем оставшийся текст
        if (!string.IsNullOrEmpty(currentText))
        {
            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, currentText));
        }
    }
};