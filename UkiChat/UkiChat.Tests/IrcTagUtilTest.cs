using UkiChat.Utils;
using Xunit;

namespace UkiChat.Tests;

public class IrcTagUtilTest
{
    [Theory]
    [InlineData(@"Watched\s7\sconsecutive\sstreams!", "Watched 7 consecutive streams!")]
    [InlineData(@"a\:b", "a;b")]
    [InlineData(@"строка\nвторая", "строка\nвторая")]
    [InlineData(@"строка\rвторая", "строка\rвторая")]
    [InlineData(@"путь\\файл", @"путь\файл")]
    [InlineData("без экранирования", "без экранирования")]
    [InlineData("", "")]
    public void Unescape_DecodesIrcV3Escapes(string raw, string expected)
    {
        Assert.Equal(expected, IrcTagUtil.Unescape(raw));
    }

    [Fact]
    public void Unescape_EscapedBackslashFollowedByS_IsNotTreatedAsSpace()
    {
        // Регрессия: цепочка Replace разворачивала \\ последней, поэтому \\s (экранированный
        // слэш + буква s) превращался в слэш с пробелом. Одиночный проход даёт верный \s.
        Assert.Equal(@"\s", IrcTagUtil.Unescape(@"\\s"));
    }

    [Fact]
    public void Unescape_EscapedBackslashFollowedByColon_IsNotTreatedAsSemicolon()
    {
        Assert.Equal(@"\:", IrcTagUtil.Unescape(@"\\:"));
    }

    [Fact]
    public void Unescape_DoubleEscapedBackslash_CollapsesOnce()
    {
        Assert.Equal(@"\\", IrcTagUtil.Unescape(@"\\\\"));
    }

    [Fact]
    public void Unescape_UnknownEscape_DropsBackslashKeepsCharacter()
    {
        // По спецификации IRCv3 неизвестная последовательность теряет слэш.
        Assert.Equal("x", IrcTagUtil.Unescape(@"\x"));
    }

    [Fact]
    public void Unescape_TrailingBackslash_IsDropped()
    {
        Assert.Equal("текст", IrcTagUtil.Unescape(@"текст\"));
    }

    [Fact]
    public void Unescape_MixedEscapes_AreDecodedLeftToRight()
    {
        Assert.Equal(@"a b;c\d", IrcTagUtil.Unescape(@"a\sb\:c\\d"));
    }
}
