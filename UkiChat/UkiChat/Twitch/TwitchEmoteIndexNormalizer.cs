using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace UkiChat.Twitch;

/// <summary>
///     Приводит индексы в теге <c>emotes=</c> сырой IRC-строки к тому виду, в котором их ждёт TwitchLib.
///     <para>
///     Twitch нумерует позиции эмоутов в КОДОВЫХ ТОЧКАХ, а TwitchLib разбирает тег через
///     <see cref="StringInfo.SubstringByTextElements(int,int)" />, то есть в ГРАФЕМАХ. Пока в тексте нет
///     комбинирующих символов, счёт совпадает. Но, например, в «Пи́сюн LUL» («и» + U+0301) 10 кодовых
///     точек и 9 графем: индексы 7-9 из тега уходят за границу, TwitchLib кидает
///     ArgumentOutOfRangeException прямо в цикле чтения сокета, и клиент молча умирает — без
///     OnDisconnected и без реконнекта (инцидент 20.07.2026, чат замер на «Пи́сюн LUL»).
///     </para>
/// </summary>
public static class TwitchEmoteIndexNormalizer
{
    /// <summary>
    ///     Возвращает строку с пересчитанными индексами эмоутов. Если пересчёт не нужен или строка
    ///     не разбирается — возвращает исходную строку без изменений.
    /// </summary>
    public static string Normalize(string rawIrc)
    {
        if (string.IsNullOrEmpty(rawIrc) || rawIrc[0] != '@')
            return rawIrc;

        var tagsEnd = rawIrc.IndexOf(' ');
        if (tagsEnd < 0)
            return rawIrc;

        var (valueStart, valueEnd) = FindEmotesTagValue(rawIrc, tagsEnd);
        if (valueStart < 0 || valueStart == valueEnd)
            return rawIrc; // тега нет или он пустой — эмоутов в сообщении нет

        var message = ExtractTrailing(rawIrc, tagsEnd);
        if (string.IsNullOrEmpty(message))
            return rawIrc;

        var elementStarts = GetTextElementStarts(message);
        var codePointToElement = BuildCodePointToElementMap(message, elementStarts);

        // Счёт совпадает — трогать нечего (подавляющее большинство сообщений).
        if (codePointToElement.Length == elementStarts.Count)
            return rawIrc;

        var remapped = Remap(rawIrc.Substring(valueStart, valueEnd - valueStart), codePointToElement);
        return string.Concat(rawIrc.AsSpan(0, valueStart), remapped, rawIrc.AsSpan(valueEnd));
    }

    /// <summary>Границы значения тега emotes= в секции тегов; (-1, -1), если тега нет.</summary>
    private static (int Start, int End) FindEmotesTagValue(string rawIrc, int tagsEnd)
    {
        const string tag = "emotes=";
        var searchFrom = 1;

        while (searchFrom < tagsEnd)
        {
            var found = rawIrc.IndexOf(tag, searchFrom, tagsEnd - searchFrom, StringComparison.Ordinal);
            if (found < 0)
                return (-1, -1);

            // Тег должен начинаться сразу после '@' или ';', иначе это хвост другого тега (flags=, badge-info=).
            if (rawIrc[found - 1] is '@' or ';')
            {
                var valueStart = found + tag.Length;
                var valueEnd = rawIrc.IndexOf(';', valueStart, tagsEnd - valueStart);
                return (valueStart, valueEnd < 0 ? tagsEnd : valueEnd);
            }

            searchFrom = found + tag.Length;
        }

        return (-1, -1);
    }

    /// <summary>Текст сообщения (trailing-параметр IRC) без завершающих CR/LF.</summary>
    private static string? ExtractTrailing(string rawIrc, int tagsEnd)
    {
        // Пропускаем пробел между тегами и префиксом, иначе наткнёмся на ':' самого префикса.
        var trailing = rawIrc.IndexOf(" :", tagsEnd + 1, StringComparison.Ordinal);
        return trailing < 0 ? null : rawIrc[(trailing + 2)..].TrimEnd('\r', '\n');
    }

    /// <summary>Смещения (в символах UTF-16) начала каждого текстового элемента.</summary>
    private static List<int> GetTextElementStarts(string message)
    {
        var starts = new List<int>();
        var enumerator = StringInfo.GetTextElementEnumerator(message);
        while (enumerator.MoveNext())
            starts.Add(enumerator.ElementIndex);
        return starts;
    }

    /// <summary>Для каждой кодовой точки — номер текстового элемента, которому она принадлежит.</summary>
    private static int[] BuildCodePointToElementMap(string message, List<int> elementStarts)
    {
        var map = new List<int>(message.Length);

        for (var element = 0; element < elementStarts.Count; element++)
        {
            var begin = elementStarts[element];
            var end = element + 1 < elementStarts.Count ? elementStarts[element + 1] : message.Length;

            for (var i = begin; i < end; i += char.IsSurrogatePair(message, i) ? 2 : 1)
                map.Add(element);
        }

        return map.ToArray();
    }

    /// <summary>
    ///     Пересчитывает значение тега (<c>id:start-end,start-end/id2:...</c>) из кодовых точек в графемы.
    ///     Если хоть один индекс не отображается, тег обнуляется целиком — сообщение покажется текстом,
    ///     это лучше, чем упавший разбор.
    /// </summary>
    private static string Remap(string value, int[] codePointToElement)
    {
        var result = new StringBuilder(value.Length);

        foreach (var emote in value.Split('/'))
        {
            var colon = emote.IndexOf(':');
            if (colon <= 0)
                return "";

            if (result.Length > 0)
                result.Append('/');
            result.Append(emote, 0, colon).Append(':');

            var ranges = emote[(colon + 1)..].Split(',');
            for (var i = 0; i < ranges.Length; i++)
            {
                var dash = ranges[i].IndexOf('-');
                if (dash <= 0
                    || !int.TryParse(ranges[i][..dash], out var start)
                    || !int.TryParse(ranges[i][(dash + 1)..], out var end)
                    || start < 0 || end < start || end >= codePointToElement.Length)
                    return "";

                if (i > 0)
                    result.Append(',');
                result.Append(codePointToElement[start]).Append('-').Append(codePointToElement[end]);
            }
        }

        return result.ToString();
    }
}
