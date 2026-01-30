using System.Text.Json.Serialization;

namespace UkiChat.Model.VkVideoLive;

/// <summary>
/// Ошибка HTTP от VK Video Live API
/// </summary>
public record VkVideoLiveHttpError
{
    /// <summary>
    /// Код ошибки
    /// </summary>
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Описание ошибки
    /// </summary>
    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; init; } = string.Empty;
}
