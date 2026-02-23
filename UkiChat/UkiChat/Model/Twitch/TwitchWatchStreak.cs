using System.Collections.Generic;

namespace UkiChat.Model.Twitch;

/// <summary>
/// Серия просмотров зрителя (watch streak).
/// Twitch шлёт как USERNOTICE с msg-id=viewermilestone, msg-param-category=watch-streak.
/// TwitchLib 4.x не обрабатывает этот тип — парсим из сырого IRC через OnUnaccountedFor.
/// </summary>
public record TwitchWatchStreak(
    string DisplayName,
    string HexColor,
    int StreakCount,
    int CopoReward,
    string SystemMessage)
{
    /// <summary>
    /// Разбирает сырую IRC-строку из OnUnaccountedFor.
    /// Возвращает null если это не watch streak.
    /// </summary>
    public static TwitchWatchStreak? ParseFromRawIrc(string rawIrc)
    {
        if (!rawIrc.Contains("msg-id=viewermilestone")) return null;
        if (!rawIrc.Contains("msg-param-category=watch-streak")) return null;

        var tags = ParseIrcTags(rawIrc);

        var displayName = tags.GetValueOrDefault("display-name", "");
        var color = tags.GetValueOrDefault("color", "");
        var systemMsg = tags.GetValueOrDefault("system-msg", "").Replace("\\s", " ");

        if (!int.TryParse(tags.GetValueOrDefault("msg-param-value", ""), out var streakCount))
            return null;

        int.TryParse(tags.GetValueOrDefault("msg-param-copoReward", "0"), out var copoReward);

        return new TwitchWatchStreak(displayName, color, streakCount, copoReward, systemMsg);
    }

    /// <summary>
    /// Разбирает IRC-теги вида @key=value;key=value;... в словарь
    /// </summary>
    private static Dictionary<string, string> ParseIrcTags(string rawIrc)
    {
        var tags = new Dictionary<string, string>();
        if (!rawIrc.StartsWith('@')) return tags;

        var tagsEnd = rawIrc.IndexOf(' ');
        if (tagsEnd < 0) return tags;

        var tagsStr = rawIrc[1..tagsEnd];
        foreach (var tag in tagsStr.Split(';'))
        {
            var eq = tag.IndexOf('=');
            if (eq < 0) continue;
            tags[tag[..eq]] = tag[(eq + 1)..];
        }

        return tags;
    }
}
