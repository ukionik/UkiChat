using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TwitchLib.Client.Models;
using UkiChat.Model.Twitch;
using UkiChat.Model.VkVideoLive;
using UkiChat.Utils;

namespace UkiChat.Model.Chat;

public record UkiChatMessage(ChatPlatform Platform
    , List<string> Badges
    , string DisplayName
    , string DisplayNameColor
    , List<UkiChatMessagePart> MessageParts
    , UkiChatReplyInfo? ReplyTo = null
    , UkiChatMessageType MessageType = UkiChatMessageType.Normal)
{
    public static UkiChatMessage FromTwitchMessage(ChatMessage twitchMessage, List<string> badgeUrls, Dictionary<string, string>? thirdPartyEmotes = null)
    {
        var messageParts = ParseMessageParts(twitchMessage.Message, twitchMessage.EmoteSet, thirdPartyEmotes);
        var displayNameColor = ColorUtil.GetDisplayNameColor(twitchMessage.DisplayName, twitchMessage.HexColor);

        UkiChatReplyInfo? replyTo = null;
        var messageType = UkiChatMessageType.Normal;

        if (twitchMessage.ChatReply != null)
        {
            var parentColor = ColorUtil.GetDisplayNameColor(twitchMessage.ChatReply.ParentDisplayName, "");
            var parentMsgBody = UnescapeIrcTagValue(twitchMessage.ChatReply.ParentMsgBody);
            replyTo = new UkiChatReplyInfo(
                twitchMessage.ChatReply.ParentDisplayName,
                parentColor,
                [new UkiChatMessagePart(UkiChatMessagePartType.Text, parentMsgBody)]
            );
            messageType = UkiChatMessageType.Reply;
        }

        return new UkiChatMessage(ChatPlatform.Twitch, badgeUrls, twitchMessage.DisplayName, displayNameColor, messageParts, replyTo, messageType);
    }

    public static UkiChatMessage FromTwitchMessageNotification(string message)
    {
        return new UkiChatMessage(ChatPlatform.Twitch, [], ChatPlatform.Twitch.ToString(), "#FFFFFF",
            [new UkiChatMessagePart(UkiChatMessagePartType.Text, message)], MessageType: UkiChatMessageType.Notification);
    }

    public static UkiChatMessage FromTwitchWatchStreak(TwitchWatchStreak watchStreak)
    {
        var displayNameColor = ColorUtil.GetDisplayNameColor(watchStreak.DisplayName, watchStreak.HexColor);
        return new UkiChatMessage(ChatPlatform.Twitch, [], watchStreak.DisplayName, displayNameColor,
            [new UkiChatMessagePart(UkiChatMessagePartType.Text, watchStreak.SystemMessage)], MessageType: UkiChatMessageType.Notification);
    }

    public static UkiChatMessage FromVkVideoLiveMessage(VkVideoLiveChatMessage chatMessage)
    {
        var author = chatMessage.Data?.Author;
        var displayName = author?.DisplayName ?? author?.Nick ?? "Unknown";
        var displayNameColor = ColorUtil.GetVkVideoLiveNickColor(author?.NickColor ?? 0);

        // Собираем бейджи
        var badges = new List<string>();

        // Добавляем обычные бейджи
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
        
        // Добавляем первую роль как бейдж (если есть)
        var firstRole = author?.Roles?.FirstOrDefault();
        if (firstRole != null)
        {
            var roleUrl = firstRole.SmallUrl ?? firstRole.MediumUrl ?? firstRole.LargeUrl;
            if (!string.IsNullOrEmpty(roleUrl))
            {
                badges.Add(roleUrl);
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
    
    public static UkiChatMessage FromVkVideoLiveMessageNotification(string message)
    {
        return new UkiChatMessage(ChatPlatform.VkVideoLive, [], ChatPlatform.VkVideoLive.ToString(), "#FFFFFF",
            [new UkiChatMessagePart(UkiChatMessagePartType.Text, message)], MessageType: UkiChatMessageType.Notification);
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
            else if (item.Type == "mention")
            {
                // Упоминание пользователя - отображаем как текст с @
                var mentionName = item.DisplayName ?? item.Nick ?? item.Name ?? "";
                if (!string.IsNullOrEmpty(mentionName))
                {
                    parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, $"@{mentionName}"));
                }
            }
        }

        return parts;
    }

    private static List<UkiChatMessagePart> ParseMessageParts(string message, EmoteSet emoteSet, Dictionary<string, string>? thirdPartyEmotes = null)
    {
        var parts = new List<UkiChatMessagePart>();

        // Если нет ни Twitch, ни сторонних эмоутов, парсим только ссылки
        if (emoteSet.Emotes.Count == 0 && (thirdPartyEmotes == null || thirdPartyEmotes.Count == 0))
        {
            ParseTextWithLinks(message, parts);
            return parts;
        }

        // Сортируем Twitch эмоуты по позиции начала
        var sortedEmotes = emoteSet.Emotes.OrderBy(e => e.StartIndex).ToList();
        var currentIndex = 0;

        foreach (var emote in sortedEmotes)
        {
            // Обрабатываем текст перед Twitch эмоутом (может содержать сторонние эмоуты)
            if (emote.StartIndex > currentIndex)
            {
                var textBefore = message.Substring(currentIndex, emote.StartIndex - currentIndex);
                ParseTextWithThirdPartyEmotes(textBefore, thirdPartyEmotes, parts);
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
            ParseTextWithThirdPartyEmotes(textAfter, thirdPartyEmotes, parts);
        }

        return parts;
    }

    private static void ParseTextWithThirdPartyEmotes(string text, Dictionary<string, string>? thirdPartyEmotes, List<UkiChatMessagePart> parts)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (thirdPartyEmotes == null || thirdPartyEmotes.Count == 0)
        {
            ParseTextWithLinks(text, parts);
            return;
        }

        var words = text.Split(' ');
        var currentText = "";

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];

            if (IsUrl(word))
            {
                if (!string.IsNullOrEmpty(currentText))
                {
                    parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, currentText));
                    currentText = "";
                }

                parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Link, word));
            }
            else if (thirdPartyEmotes.TryGetValue(word, out var emoteUrl))
            {
                if (!string.IsNullOrEmpty(currentText))
                {
                    parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, currentText));
                    currentText = "";
                }

                parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Emote, emoteUrl));
            }
            else
            {
                currentText += word;
            }

            if (i < words.Length - 1)
                currentText += " ";
        }

        if (!string.IsNullOrEmpty(currentText))
            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, currentText));
    }

    private static void ParseTextWithLinks(string text, List<UkiChatMessagePart> parts)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var words = text.Split(' ');
        var currentText = "";

        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];

            if (IsUrl(word))
            {
                if (!string.IsNullOrEmpty(currentText))
                {
                    parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, currentText));
                    currentText = "";
                }

                parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Link, word));
            }
            else
            {
                currentText += word;
            }

            if (i < words.Length - 1)
                currentText += " ";
        }

        if (!string.IsNullOrEmpty(currentText))
            parts.Add(new UkiChatMessagePart(UkiChatMessagePartType.Text, currentText));
    }

    private static bool IsUrl(string word) =>
        word.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        word.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    // IRCv3 message tags кодируют спецсимволы: \s=пробел, \\=\, \:=;, \n=LF, \r=CR
    private static string UnescapeIrcTagValue(string value) =>
        value.Replace(@"\s", " ").Replace(@"\:", ";").Replace(@"\n", "\n").Replace(@"\r", "\r").Replace(@"\\", "\\");
}

/// <summary>
/// Информация об ответе на сообщение
/// </summary>
public record UkiChatReplyInfo(
    string DisplayName,
    string DisplayNameColor,
    List<UkiChatMessagePart> MessageParts
);