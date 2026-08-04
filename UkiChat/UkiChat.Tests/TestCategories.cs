namespace UkiChat.Tests;

/// <summary>
///     Категории тестов для фильтрации при запуске.
///     Юнит-тесты идут без атрибута и выполняются всегда:
///     <code>dotnet test --filter "Category!=Integration"</code>
///     Интеграционные требуют сети и заполненного app-settings.local.toml,
///     часть из них намеренно долгие (слушают живой чат).
/// </summary>
public static class TestCategories
{
    public const string Category = "Category";
    public const string Integration = "Integration";
}
