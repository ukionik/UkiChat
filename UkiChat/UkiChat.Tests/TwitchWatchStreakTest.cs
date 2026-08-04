using UkiChat.Model.Twitch;
using Xunit;

namespace UkiChat.Tests;

/// <summary>
///     Разбор watch streak из сырой IRC-строки. TwitchLib этот USERNOTICE не обрабатывает,
///     поэтому парсер наш и полностью на нашей ответственности.
/// </summary>
public class TwitchWatchStreakTest
{
    private const string ValidRawIrc =
        @"@badge-info=;badges=;color=#FF0000;display-name=TestUser;msg-id=viewermilestone;" +
        @"msg-param-category=watch-streak;msg-param-value=7;msg-param-copoReward=350;" +
        @"system-msg=Watched\s7\sconsecutive\sstreams! :tmi.twitch.tv USERNOTICE #channel";

    [Fact]
    public void ParseFromRawIrc_ValidWatchStreak_ParsesAllFields()
    {
        var streak = TwitchWatchStreak.ParseFromRawIrc(ValidRawIrc);

        Assert.NotNull(streak);
        Assert.Equal("TestUser", streak.DisplayName);
        Assert.Equal("#FF0000", streak.HexColor);
        Assert.Equal(7, streak.StreakCount);
        Assert.Equal(350, streak.CopoReward);
        // \s в теге — это пробел (IRCv3), парсер обязан его развернуть.
        Assert.Equal("Watched 7 consecutive streams!", streak.SystemMessage);
    }

    [Fact]
    public void ParseFromRawIrc_OrdinaryMessage_ReturnsNull()
    {
        const string privmsg =
            "@display-name=TestUser;color=#FF0000 :user!user@user.tmi.twitch.tv PRIVMSG #channel :привет";

        Assert.Null(TwitchWatchStreak.ParseFromRawIrc(privmsg));
    }

    [Fact]
    public void ParseFromRawIrc_OtherMilestoneCategory_ReturnsNull()
    {
        var raw = ValidRawIrc.Replace("msg-param-category=watch-streak", "msg-param-category=something-else");

        Assert.Null(TwitchWatchStreak.ParseFromRawIrc(raw));
    }

    [Fact]
    public void ParseFromRawIrc_OtherMsgId_ReturnsNull()
    {
        var raw = ValidRawIrc.Replace("msg-id=viewermilestone", "msg-id=sub");

        Assert.Null(TwitchWatchStreak.ParseFromRawIrc(raw));
    }

    [Fact]
    public void ParseFromRawIrc_MissingStreakValue_ReturnsNull()
    {
        // Без msg-param-value показывать нечего — сообщение отбрасывается целиком.
        var raw = ValidRawIrc.Replace("msg-param-value=7;", "");

        Assert.Null(TwitchWatchStreak.ParseFromRawIrc(raw));
    }

    [Fact]
    public void ParseFromRawIrc_NonNumericStreakValue_ReturnsNull()
    {
        var raw = ValidRawIrc.Replace("msg-param-value=7;", "msg-param-value=abc;");

        Assert.Null(TwitchWatchStreak.ParseFromRawIrc(raw));
    }

    [Fact]
    public void ParseFromRawIrc_MissingCopoReward_DefaultsToZero()
    {
        // Награда необязательна — в отличие от streakCount, её отсутствие не отменяет событие.
        var raw = ValidRawIrc.Replace("msg-param-copoReward=350;", "");

        var streak = TwitchWatchStreak.ParseFromRawIrc(raw);

        Assert.NotNull(streak);
        Assert.Equal(0, streak.CopoReward);
        Assert.Equal(7, streak.StreakCount);
    }

    [Fact]
    public void ParseFromRawIrc_MissingOptionalTags_UsesEmptyStrings()
    {
        var raw = ValidRawIrc
            .Replace("display-name=TestUser;", "")
            .Replace("color=#FF0000;", "");

        var streak = TwitchWatchStreak.ParseFromRawIrc(raw);

        Assert.NotNull(streak);
        Assert.Equal("", streak.DisplayName);
        Assert.Equal("", streak.HexColor);
    }

    [Fact]
    public void ParseFromRawIrc_WithoutTagPrefix_ReturnsNull()
    {
        // Строка без '@' не содержит тегов: msg-param-value не найдётся → null.
        var raw = ValidRawIrc.TrimStart('@');

        Assert.Null(TwitchWatchStreak.ParseFromRawIrc(raw));
    }
}
