namespace UkiChat.Data.DefaultAppSettingsData;

public record DefaultAppSettingsDatabase
{
    public string Filename { get; init; }
    // Пароля здесь намеренно нет: этот файл вшит в сборку как EmbeddedResource и
    // достаётся из DLL открытым текстом. Ключ БД генерируется на месте — см. DatabaseKeyProvider.
}