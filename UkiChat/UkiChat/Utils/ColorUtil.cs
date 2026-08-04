using System.Text;

namespace UkiChat.Utils;

public static class ColorUtil
{
    // Цвета, которые Twitch использует по умолчанию для имен пользователей
    private static readonly string[] DefaultColors =
    [
        "#FF0000", "#0000FF", "#00FF00", "#B22222", "#FF7F50",
        "#9ACD32", "#FF4500", "#2E8B57", "#DAA520", "#D2691E",
        "#5F9EA0", "#1E90FF", "#FF69B4", "#8A2BE2", "#00FF7F"
    ];

    // Цвета ников VK Video Live (индексы 0-15)
    private static readonly string[] VkVideoLiveColors =
    [
        "#D66E34", "#B8AAFF", "#1D90FF", "#9961F9",
        "#59A840", "#E73629", "#DE6489", "#20BBA1",
        "#F8B301", "#0099BB", "#7BBEFF", "#E542FF",
        "#A36C59", "#8BA259", "#00A9FF", "#A20BFF"
    ];

    /// <summary>
    ///     Возвращает цвет для отображения имени пользователя.
    ///     Если hexColor задан, возвращает его, иначе подбирает цвет по самому имени.
    ///     Подбор детерминированный: один и тот же ник всегда получает один и тот же цвет —
    ///     и между запусками приложения, и на любой площадке.
    /// </summary>
    public static string GetDisplayNameColor(string displayName, string? hexColor = null)
    {
        // Если цвет задан и не пустой, используем его
        if (!string.IsNullOrEmpty(hexColor))
            return hexColor;

        var index = (int)(StableHash(displayName) % (uint)DefaultColors.Length);
        return DefaultColors[index];
    }

    /// <summary>
    ///     Возвращает цвет ника VK Video Live по индексу палитры площадки (0-15).
    ///     Если индекс не задан или вне диапазона, цвет подбирается по нику — так зритель
    ///     без цвета выглядит одинаково на всех площадках.
    /// </summary>
    public static string GetVkVideoLiveNickColor(int? colorIndex, string displayName)
    {
        if (colorIndex is null || colorIndex < 0 || colorIndex >= VkVideoLiveColors.Length)
            return GetDisplayNameColor(displayName);

        return VkVideoLiveColors[colorIndex.Value];
    }

    /// <summary>
    ///     FNV-1a поверх UTF-8 байт имени в нижнем регистре.
    ///     Раньше здесь был string.GetHashCode(), а он в .NET рандомизируется ПРИ КАЖДОМ ЗАПУСКЕ
    ///     процесса: цвет ника менялся после каждого перезапуска приложения. Своя хэш-функция
    ///     нужна именно ради воспроизводимости — криптостойкость тут ни при чём.
    ///     Регистр приводится к нижнему, чтобы один и тот же человек получал один цвет,
    ///     даже если площадки пишут его ник по-разному ("UkiOnik" / "ukionik").
    /// </summary>
    private static uint StableHash(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var b in Encoding.UTF8.GetBytes(value.ToLowerInvariant()))
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }
}
