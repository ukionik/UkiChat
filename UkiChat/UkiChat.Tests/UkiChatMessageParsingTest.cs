using System.Collections.Generic;
using UkiChat.Model.Chat;
using Xunit;

namespace UkiChat.Tests;

/// <summary>
///     Фиксирует ТЕКУЩЕЕ поведение разбора текста сообщения на части (текст / ссылка / эмоут).
///     Это характеризующие тесты: они написаны перед рефакторингом эмоут-провайдеров (Фаза 1),
///     чтобы перенос кода не изменил результат незаметно. Отсюда дотошность к пробелам —
///     разбиение по словам оставляет их именно так, и фронтенд на это рассчитывает.
/// </summary>
public class UkiChatMessageParsingTest
{
    private static List<UkiChatMessagePart> ParseLinks(string text)
    {
        var parts = new List<UkiChatMessagePart>();
        UkiChatMessage.ParseTextWithLinks(text, parts);
        return parts;
    }

    private static List<UkiChatMessagePart> ParseEmotes(string text, Dictionary<string, string>? emotes)
    {
        var parts = new List<UkiChatMessagePart>();
        UkiChatMessage.ParseTextWithThirdPartyEmotes(text, emotes, parts);
        return parts;
    }

    private static void AssertPart(UkiChatMessagePart part, UkiChatMessagePartType type, string content)
    {
        Assert.Equal(type, part.Type);
        Assert.Equal(content, part.Content);
    }

    // ---------- ParseTextWithLinks ----------

    [Fact]
    public void ParseTextWithLinks_EmptyText_AddsNothing()
    {
        Assert.Empty(ParseLinks(""));
    }

    [Fact]
    public void ParseTextWithLinks_PlainText_ReturnsSingleTextPart()
    {
        var parts = ParseLinks("привет всем в чате");

        var part = Assert.Single(parts);
        AssertPart(part, UkiChatMessagePartType.Text, "привет всем в чате");
    }

    [Fact]
    public void ParseTextWithLinks_UrlOnly_ReturnsSingleLinkPart()
    {
        var parts = ParseLinks("https://twitch.tv/ukionik");

        var part = Assert.Single(parts);
        AssertPart(part, UkiChatMessagePartType.Link, "https://twitch.tv/ukionik");
    }

    [Fact]
    public void ParseTextWithLinks_UrlInsideText_SplitsKeepingSurroundingSpaces()
    {
        var parts = ParseLinks("смотри https://twitch.tv/ukionik сейчас");

        Assert.Equal(3, parts.Count);
        // Пробел-разделитель уезжает в ХВОСТ предыдущего текста и в НАЧАЛО следующего.
        AssertPart(parts[0], UkiChatMessagePartType.Text, "смотри ");
        AssertPart(parts[1], UkiChatMessagePartType.Link, "https://twitch.tv/ukionik");
        AssertPart(parts[2], UkiChatMessagePartType.Text, " сейчас");
    }

    [Fact]
    public void ParseTextWithLinks_TwoAdjacentUrls_KeepsSeparatorAsTextPart()
    {
        var parts = ParseLinks("https://a.com https://b.com");

        Assert.Equal(3, parts.Count);
        AssertPart(parts[0], UkiChatMessagePartType.Link, "https://a.com");
        AssertPart(parts[1], UkiChatMessagePartType.Text, " ");
        AssertPart(parts[2], UkiChatMessagePartType.Link, "https://b.com");
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("HTTPS://EXAMPLE.COM")]
    [InlineData("HtTp://example.com")]
    public void ParseTextWithLinks_SchemeIsCaseInsensitive(string url)
    {
        var part = Assert.Single(ParseLinks(url));
        AssertPart(part, UkiChatMessagePartType.Link, url);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("www.example.com")]
    public void ParseTextWithLinks_NonHttpScheme_StaysText(string word)
    {
        var part = Assert.Single(ParseLinks(word));
        AssertPart(part, UkiChatMessagePartType.Text, word);
    }

    // ---------- ParseTextWithThirdPartyEmotes ----------

    [Fact]
    public void ParseTextWithThirdPartyEmotes_NullDictionary_FallsBackToLinkParsing()
    {
        var parts = ParseEmotes("смотри https://a.com", null);

        Assert.Equal(2, parts.Count);
        AssertPart(parts[0], UkiChatMessagePartType.Text, "смотри ");
        AssertPart(parts[1], UkiChatMessagePartType.Link, "https://a.com");
    }

    [Fact]
    public void ParseTextWithThirdPartyEmotes_EmptyDictionary_FallsBackToLinkParsing()
    {
        var parts = ParseEmotes("просто текст", new Dictionary<string, string>());

        var part = Assert.Single(parts);
        AssertPart(part, UkiChatMessagePartType.Text, "просто текст");
    }

    [Fact]
    public void ParseTextWithThirdPartyEmotes_EmoteOnly_ReturnsSingleEmotePart()
    {
        var emotes = new Dictionary<string, string> { ["Kappa"] = "https://cdn/kappa.webp" };

        var part = Assert.Single(ParseEmotes("Kappa", emotes));
        AssertPart(part, UkiChatMessagePartType.Emote, "https://cdn/kappa.webp");
    }

    [Fact]
    public void ParseTextWithThirdPartyEmotes_EmoteInsideText_SplitsKeepingSurroundingSpaces()
    {
        var emotes = new Dictionary<string, string> { ["Kappa"] = "https://cdn/kappa.webp" };

        var parts = ParseEmotes("привет Kappa всем", emotes);

        Assert.Equal(3, parts.Count);
        AssertPart(parts[0], UkiChatMessagePartType.Text, "привет ");
        AssertPart(parts[1], UkiChatMessagePartType.Emote, "https://cdn/kappa.webp");
        AssertPart(parts[2], UkiChatMessagePartType.Text, " всем");
    }

    [Fact]
    public void ParseTextWithThirdPartyEmotes_MatchesWholeWordOnly()
    {
        var emotes = new Dictionary<string, string> { ["Kappa"] = "https://cdn/kappa.webp" };

        // "KappaHD" — другое слово, подстановки быть не должно.
        var part = Assert.Single(ParseEmotes("KappaHD", emotes));
        AssertPart(part, UkiChatMessagePartType.Text, "KappaHD");
    }

    [Fact]
    public void ParseTextWithThirdPartyEmotes_IsCaseSensitive()
    {
        var emotes = new Dictionary<string, string> { ["Kappa"] = "https://cdn/kappa.webp" };

        var part = Assert.Single(ParseEmotes("kappa", emotes));
        AssertPart(part, UkiChatMessagePartType.Text, "kappa");
    }

    [Fact]
    public void ParseTextWithThirdPartyEmotes_UrlWinsOverEmoteLookup()
    {
        // Ссылка проверяется первой, поэтому слово-ссылка не станет эмоутом,
        // даже если оно есть в словаре.
        var emotes = new Dictionary<string, string> { ["https://a.com"] = "https://cdn/trap.webp" };

        var part = Assert.Single(ParseEmotes("https://a.com", emotes));
        AssertPart(part, UkiChatMessagePartType.Link, "https://a.com");
    }

    [Fact]
    public void ParseTextWithThirdPartyEmotes_ConsecutiveEmotes_KeepSeparatorAsTextPart()
    {
        var emotes = new Dictionary<string, string>
        {
            ["Kappa"] = "https://cdn/kappa.webp",
            ["PogChamp"] = "https://cdn/pog.webp"
        };

        var parts = ParseEmotes("Kappa PogChamp", emotes);

        Assert.Equal(3, parts.Count);
        AssertPart(parts[0], UkiChatMessagePartType.Emote, "https://cdn/kappa.webp");
        AssertPart(parts[1], UkiChatMessagePartType.Text, " ");
        AssertPart(parts[2], UkiChatMessagePartType.Emote, "https://cdn/pog.webp");
    }

    [Fact]
    public void ParseTextWithThirdPartyEmotes_MixOfEmoteAndLink()
    {
        var emotes = new Dictionary<string, string> { ["Kappa"] = "https://cdn/kappa.webp" };

        var parts = ParseEmotes("Kappa тут https://a.com", emotes);

        Assert.Equal(3, parts.Count);
        AssertPart(parts[0], UkiChatMessagePartType.Emote, "https://cdn/kappa.webp");
        AssertPart(parts[1], UkiChatMessagePartType.Text, " тут ");
        AssertPart(parts[2], UkiChatMessagePartType.Link, "https://a.com");
    }

    // ---------- FormatDonationAmount ----------

    [Theory]
    [InlineData(100d, "RUB", "100 ₽")]
    [InlineData(100d, "USD", "100 $")]
    [InlineData(100d, "EUR", "100 €")]
    [InlineData(100d, "UAH", "100 ₴")]
    [InlineData(100d, "BRL", "100 R$")]
    public void FormatDonationAmount_KnownCurrencies_UseSymbol(double amount, string currency, string expected)
    {
        Assert.Equal(expected, UkiChatMessage.FormatDonationAmount(amount, currency));
    }

    [Fact]
    public void FormatDonationAmount_CurrencyCodeIsCaseInsensitive()
    {
        Assert.Equal("100 ₽", UkiChatMessage.FormatDonationAmount(100d, "rub"));
    }

    [Fact]
    public void FormatDonationAmount_UnknownCurrency_KeepsCodeAsIs()
    {
        Assert.Equal("100 JPY", UkiChatMessage.FormatDonationAmount(100d, "JPY"));
    }

    [Fact]
    public void FormatDonationAmount_EmptyCurrency_HasNoTrailingSpace()
    {
        Assert.Equal("100", UkiChatMessage.FormatDonationAmount(100d, ""));
    }

    [Theory]
    [InlineData(100d, "100 ₽")]        // целое — без дробной части
    [InlineData(99.5d, "99.50 ₽")]     // дробное — ровно две цифры
    [InlineData(0.5d, "0.50 ₽")]
    [InlineData(1234.56d, "1234.56 ₽")] // разделителя тысяч нет
    public void FormatDonationAmount_FormatsFractionalPart(double amount, string expected)
    {
        Assert.Equal(expected, UkiChatMessage.FormatDonationAmount(amount, "RUB"));
    }
}
