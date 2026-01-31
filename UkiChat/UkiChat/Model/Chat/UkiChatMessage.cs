using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TwitchLib.Client.Models;
using UkiChat.Model.SevenTv;
using UkiChat.Model.VkVideoLive;
using UkiChat.Utils;

namespace UkiChat.Model.Chat;

public record UkiChatMessage(ChatPlatform Platform
    , List<string> Badges
    , string DisplayName
    , string DisplayNameColor
    , List<UkiChatMessagePart> MessageParts
    , UkiChatReplyInfo? ReplyTo = null)
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

    public static UkiChatMessage FromVkVideoLiveMessage(VkVideoLiveChatMessage chatMessage)
    {
        var author = chatMessage.Data?.Author;
        var displayName = author?.DisplayName ?? author?.Nick ?? "Unknown";
        var displayNameColor = ColorUtil.GetVkVideoLiveNickColor(author?.NickColor ?? 0);

        // Собираем бейджи
        var badges = new List<string>();
        if (author?.Badges != null)
        {
            foreach (var badge in author.Badges)
            {
                var badgeUrl = badge.SmallUrl ?? badge.MediumUrl ?? badge.LargeUrl;
                if (!string.IsNullOrEmpty(badgeUrl))
                {
                    badges.Add(badgeUrl);
                }
            }
        }

        // Парсим контент сообщения
        var messageParts = ParseVkVideoLiveContent(chatMessage.Data?.Content);

        // Парсим информацию об ответе (если есть)
        UkiChatReplyInfo? replyTo = null;
        var parent = chatMessage.Data?.Parent;
        if (parent != null)
        {
            var parentAuthor = parent.Author;
            var parentDisplayName = parentAuthor?.DisplayName ?? parentAuthor?.Nick ?? "Unknown";
            var parentDisplayNameColor = ColorUtil.GetVkVideoLiveNickColor(parentAuthor?.NickColor ?? 0);
            var parentMessageParts = ParseVkVideoLiveContent(parent.Content);

            replyTo = new UkiChatReplyInfo(parentDisplayName, parentDisplayNameColor, parentMessageParts);
        }

        return new UkiChatMessage(ChatPlatform.VkVideoLive, badges, displayName, displayNameColor, messageParts, replyTo);
    }

    private static List<UkiChatMessagePart> ParseVkVideoLiveContent(List<VkVideoLiveChatContent>? content)
    {
        var parts = new List<UkiChatMessagePart>();

        if (content == null || content.Count == 0)
        {
            return parts;
        }

        foreach (var item in content)
        {
            // Пропускаем BLOCK_END маркеры
            if (item.Modificator == "BLOCK_END")
                continue;

            if (item.Type == "text" && !string.IsNullOrEmpty(item.Content))
            {
                // Контент в формате ["текст","стиль",[]]
                try
                {
                    using var doc = JsonDocument.Parse(item.Content);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                    {
                        var text = doc.RootElement[0].GetString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, text));
                        }
                    }
                }
                catch
                {
                    // Если не JSON, добавляем как есть
                    parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, item.Content));
                }
            }
            else if (item.Type == "smile")
            {
                // Эмоут - используем URL картинки (предпочитаем medium размер)
                var emoteUrl = item.MediumUrl ?? item.SmallUrl ?? item.LargeUrl;
                if (!string.IsNullOrEmpty(emoteUrl))
                {
                    parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Emote, emoteUrl));
                }
            }
        }

        return parts;
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
}

/// <summary>
/// Информация об ответе на сообщение
/// </summary>
public record UkiChatReplyInfo(
    string DisplayName,
    string DisplayNameColor,
    List<UkiChatMessagePart> MessageParts
);