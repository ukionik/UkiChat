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
    /// Возвращает цвет для отображения имени пользователя.
    /// Если hexColor задан, возвращает его, иначе генерирует цвет на основе хэша displayName.
    /// </summary>
    public static string GetDisplayNameColor(string displayName, string? hexColor = null)
    {
        // Если цвет задан и не пустой, используем его
        if (!string.IsNullOrEmpty(hexColor))
            return hexColor;

        // Иначе генерируем цвет на основе хэша имени пользователя
        var hash = displayName.GetHashCode();
        var index = (hash & 0x7FFFFFFF) % DefaultColors.Length; // Используем абсолютное значение
        return DefaultColors[index];
    }

    /// <summary>
    /// Возвращает цвет ника VK Video Live по индексу (0-15).
    /// </summary>
    public static string GetVkVideoLiveNickColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= VkVideoLiveColors.Length)
            return VkVideoLiveColors[0]; // Возвращаем первый цвет по умолчанию

        return VkVideoLiveColors[colorIndex];
    }
}
