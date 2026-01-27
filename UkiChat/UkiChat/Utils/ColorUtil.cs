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
}
