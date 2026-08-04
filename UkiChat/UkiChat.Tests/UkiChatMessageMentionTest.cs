using System.Collections.Generic;
using UkiChat.Model.Chat;
using Xunit;

namespace UkiChat.Tests;

/// <summary>
///     Проверка пометки сообщения как упоминания (<see cref="UkiChatMessage.WithMentionCheck" />).
///     Логика построена на регулярке с границами слова — тесты фиксируют именно эти границы,
///     потому что ложное срабатывание («uki» внутри «ukichat») подсвечивает чат впустую.
/// </summary>
public class UkiChatMessageMentionTest
{
    private static UkiChatMessage TextMessage(string text,
        UkiChatMessageType messageType = UkiChatMessageType.Normal)
    {
        return new UkiChatMessage(ChatPlatform.Twitch, [], "Viewer", "#FFFFFF",
            [new UkiChatMessagePart(UkiChatMessagePartType.Text, text)], MessageType: messageType);
    }

    [Fact]
    public void WithMentionCheck_NickAsSeparateWord_MarksAsMention()
    {
        var result = TextMessage("привет uki как дела").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NickWithAtSign_MarksAsMention()
    {
        var result = TextMessage("@uki привет").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NickConfiguredWithAtSign_MarksAsMention()
    {
        // Ник в настройках можно задать и как "@uki" — ведущая «собака» отбрасывается.
        var result = TextMessage("привет uki").WithMentionCheck(["@uki"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_IsCaseInsensitive()
    {
        var result = TextMessage("привет UKI").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NickAsPrefixOfAnotherWord_DoesNotMatch()
    {
        // Главный смысл границы слова: "ukichat" — не упоминание "uki".
        var result = TextMessage("ukichat лучший").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NickAsSuffixOfAnotherWord_DoesNotMatch()
    {
        // Обратная граница — просмотр назад: "своиuki" тоже не упоминание.
        var result = TextMessage("своиuki").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_CyrillicNickInsideWord_DoesNotMatch()
    {
        // Границы слова должны работать и на кириллице: "муки" не содержит упоминания "уки".
        var result = TextMessage("одни муки").WithMentionCheck(["уки"]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NickFollowedByPunctuation_MarksAsMention()
    {
        var result = TextMessage("uki, привет!").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NoNickInText_StaysNormal()
    {
        var result = TextMessage("обычное сообщение").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_EmptyNicknameList_StaysNormal()
    {
        var result = TextMessage("привет uki").WithMentionCheck([]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_BlankNicknamesAreSkipped()
    {
        var result = TextMessage("привет uki").WithMentionCheck(["", "@", "uki"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_AnyOfSeveralNicknamesMatches()
    {
        var result = TextMessage("привет второй").WithMentionCheck(["первый", "второй"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Theory]
    [InlineData(UkiChatMessageType.Notification)]
    [InlineData(UkiChatMessageType.Reply)]
    [InlineData(UkiChatMessageType.Donation)]
    [InlineData(UkiChatMessageType.Subscription)]
    [InlineData(UkiChatMessageType.Raid)]
    [InlineData(UkiChatMessageType.Cheer)]
    [InlineData(UkiChatMessageType.ChannelPointsRedemption)]
    public void WithMentionCheck_NonNormalMessageType_IsLeftUntouched(UkiChatMessageType messageType)
    {
        // Тип уже несёт смысл (донат, рейд, ответ) — перебивать его упоминанием нельзя.
        var result = TextMessage("привет uki", messageType).WithMentionCheck(["uki"]);

        Assert.Equal(messageType, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NickInsideEmotePart_IsIgnored()
    {
        // Совпадение ищется только по текстовым частям: URL эмоута может случайно
        // содержать ник, и подсвечивать сообщение из-за этого не нужно.
        var message = new UkiChatMessage(ChatPlatform.Twitch, [], "Viewer", "#FFFFFF",
            [new UkiChatMessagePart(UkiChatMessagePartType.Emote, "https://cdn/uki.webp")]);

        var result = message.WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NicknameWithRegexMetacharacters_IsMatchedLiterally()
    {
        // Ник экранируется (Regex.Escape), поэтому точка ищется как точка.
        var result = TextMessage("привет uki.chat там").WithMentionCheck(["uki.chat"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_RegexMetacharacterDoesNotActAsWildcard()
    {
        // Если бы точка не экранировалась, "ukiXchat" совпал бы с "uki.chat".
        var result = TextMessage("привет ukiXchat там").WithMentionCheck(["uki.chat"]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NicknameEndingInNonWordCharacter_MatchesToo()
    {
        // Раньше шаблон заканчивался на \b, а после "+" границы слова нет — такой ник
        // не срабатывал никогда. Теперь справа стоит просмотр вперёд, симметричный левому.
        var result = TextMessage("привет c++ там").WithMentionCheck(["c++"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NicknameEndingInNonWordCharacter_StillRespectsRightBoundary()
    {
        // Симметрия не должна ломать главное: "c++" внутри "c++builder" — не упоминание.
        var result = TextMessage("привет c++builder там").WithMentionCheck(["c++"]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NickFollowedByUnderscore_DoesNotMatch()
    {
        // Подчёркивание — часть ника на площадках, поэтому "uki_bot" не упоминает "uki".
        var result = TextMessage("привет uki_bot").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NickFollowedByDigit_DoesNotMatch()
    {
        var result = TextMessage("привет uki2").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Normal, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_NickAtEndOfText_MarksAsMention()
    {
        var result = TextMessage("всем привет uki").WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
    }

    [Fact]
    public void WithMentionCheck_PreservesEverythingExceptMessageType()
    {
        var original = TextMessage("привет uki");

        var result = original.WithMentionCheck(["uki"]);

        Assert.Equal(UkiChatMessageType.Mention, result.MessageType);
        Assert.Equal(original.DisplayName, result.DisplayName);
        Assert.Equal(original.DisplayNameColor, result.DisplayNameColor);
        Assert.Equal(original.Platform, result.Platform);
        Assert.Same(original.MessageParts, result.MessageParts);
        // Исходное сообщение не мутируется — record возвращает копию.
        Assert.Equal(UkiChatMessageType.Normal, original.MessageType);
    }
}
