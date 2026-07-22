using TwitchLib.Client.Models;
using UkiChat.Twitch;
using Xunit;

namespace UkiChat.Tests;

public class TwitchEmoteIndexNormalizerTest
{
    private static string Raw(string emotesTag, string message) =>
        $"@badge-info=;badges=;color=;display-name=kytenok0_0;emotes={emotesTag};first-msg=0;mod=0;room-id=40689900 " +
        $":kytenok0_0!kytenok0_0@kytenok0_0.tmi.twitch.tv PRIVMSG #konataityan :{message}\r\n";

    private static string EmotesTagOf(string rawIrc)
    {
        var start = rawIrc.IndexOf("emotes=", System.StringComparison.Ordinal) + "emotes=".Length;
        return rawIrc[start..rawIrc.IndexOf(';', start)];
    }

    /// <summary>Сообщение, на котором 20.07.2026 замер чат: «и» + комбинирующий U+0301.</summary>
    [Fact]
    public void ПересчитываетИндексыПриКомбинирующемСимволе()
    {
        var raw = Raw("425618:7-9", "Пи́сюн LUL");

        var normalized = TwitchEmoteIndexNormalizer.Normalize(raw);

        Assert.Equal("425618:6-8", EmotesTagOf(normalized));
    }

    /// <summary>После пересчёта TwitchLib разбирает сообщение вместо ArgumentOutOfRangeException.</summary>
    [Fact]
    public void ПослеПересчётаTwitchLibРазбираетЭмоут()
    {
        const string message = "Пи́сюн LUL";
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new EmoteSet("425618:7-9", message));

        var tag = EmotesTagOf(TwitchEmoteIndexNormalizer.Normalize(Raw("425618:7-9", message)));
        var emote = Assert.Single(new EmoteSet(tag, message).Emotes);

        Assert.Equal("LUL", emote.Name);
        // Индексы наружу TwitchLib отдаёт уже в символах UTF-16 — на них опирается разбор сообщения.
        Assert.Equal(7, emote.StartIndex);
        Assert.Equal(9, emote.EndIndex);
    }

    [Fact]
    public void НесколькоЭмоутовИДиапазонов()
    {
        // «Пи́сюн LUL Kappa LUL»: LUL на 7-9 и 17-19, Kappa на 11-15 (в кодовых точках).
        var raw = Raw("425618:7-9,17-19/25:11-15", "Пи́сюн LUL Kappa LUL");

        Assert.Equal("425618:6-8,16-18/25:10-14", EmotesTagOf(TwitchEmoteIndexNormalizer.Normalize(raw)));
    }

    [Fact]
    public void ОбычноеСообщениеНеТрогает()
    {
        var raw = Raw("425618:15-17", "Хорошо не бобу LUL");

        Assert.Same(raw, TwitchEmoteIndexNormalizer.Normalize(raw));
    }

    /// <summary>Суррогатные пары считаются одинаково и там, и там — пересчёт не нужен.</summary>
    [Fact]
    public void СуррогатнуюПаруНеТрогает()
    {
        var raw = Raw("425618:2-4", "\U0001F600 LUL");

        Assert.Same(raw, TwitchEmoteIndexNormalizer.Normalize(raw));
    }

    /// <summary>Составной эмодзи (ZWJ) — одна графема, но пять кодовых точек.</summary>
    [Fact]
    public void ПересчитываетИндексыПриСоставномЭмодзи()
    {
        var raw = Raw("425618:6-8", "\U0001F468‍\U0001F469‍\U0001F467 LUL");

        Assert.Equal("425618:2-4", EmotesTagOf(TwitchEmoteIndexNormalizer.Normalize(raw)));
    }

    [Fact]
    public void ПустойИлиОтсутствующийТегНеТрогает()
    {
        var withoutEmotes = Raw("", "Пи́сюн");
        var noTagAtAll = ":kytenok0_0!kytenok0_0@kytenok0_0.tmi.twitch.tv PRIVMSG #konataityan :Пи́сюн\r\n";

        Assert.Same(withoutEmotes, TwitchEmoteIndexNormalizer.Normalize(withoutEmotes));
        Assert.Same(noTagAtAll, TwitchEmoteIndexNormalizer.Normalize(noTagAtAll));
    }

    /// <summary>Битые индексы гасим целиком — сообщение покажется текстом, но парсер не упадёт.</summary>
    [Fact]
    public void ИндексЗаГраницейСообщенияОбнуляетТег()
    {
        var raw = Raw("425618:40-42", "Пи́сюн LUL");

        Assert.Equal("", EmotesTagOf(TwitchEmoteIndexNormalizer.Normalize(raw)));
    }

    /// <summary>Тег flags= содержит подстроку «emotes=»-подобных значений — не должен сбивать поиск.</summary>
    [Fact]
    public void НеПутаетТегСХвостомДругогоТега()
    {
        var raw = "@badge-info=;badges=;client-nonce=abc;color=;display-name=kytenok0_0;emote-only=1;" +
                  "emotes=425618:7-9;first-msg=0;flags=0-11:P.6/S.7;mod=0 " +
                  ":kytenok0_0!kytenok0_0@kytenok0_0.tmi.twitch.tv PRIVMSG #konataityan :Пи́сюн LUL\r\n";

        Assert.Contains("emotes=425618:6-8;", TwitchEmoteIndexNormalizer.Normalize(raw));
    }
}
