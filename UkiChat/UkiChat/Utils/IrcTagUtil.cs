using System.Text;

namespace UkiChat.Utils;

public static class IrcTagUtil
{
    /// <summary>
    ///     Разворачивает escape-последовательности значения IRCv3-тега:
    ///     <c>\s</c> = пробел, <c>\:</c> = точка с запятой, <c>\n</c> = LF, <c>\r</c> = CR,
    ///     <c>\\</c> = обратный слэш.
    ///     Разбор идёт одним проходом слева направо. Цепочка Replace здесь не годится:
    ///     обратный слэш разворачивался последним, и вход <c>\\s</c> (экранированный слэш плюс
    ///     буква s) превращался в слэш с пробелом вместо <c>\s</c>.
    ///     По спецификации неизвестная последовательность теряет слэш и оставляет символ,
    ///     а одиночный слэш в конце значения отбрасывается.
    /// </summary>
    public static string Unescape(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('\\'))
            return value;

        var result = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\')
            {
                result.Append(value[i]);
                continue;
            }

            // Слэш последним символом — отбрасываем.
            if (i + 1 >= value.Length)
                break;

            i++;
            result.Append(value[i] switch
            {
                's' => ' ',
                ':' => ';',
                'n' => '\n',
                'r' => '\r',
                '\\' => '\\',
                var other => other
            });
        }

        return result.ToString();
    }
}
