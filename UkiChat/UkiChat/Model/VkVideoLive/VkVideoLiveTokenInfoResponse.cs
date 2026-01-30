using System.Text.Json.Serialization;

namespace UkiChat.Model.VkVideoLive;

/// <summary>
/// Ответ от VK Video Live API при проверке токена
/// </summary>
public record VkVideoLiveTokenInfoResponse
{
    /// <summary>
    /// Данные о токене
    /// </summary>
    [JsonPropertyName("data")]
    public VkVideoLiveTokenInfo Data { get; init; } = new();
}

/// <summary>
/// Информация о токене
/// </summary>
public record VkVideoLiveTokenInfo
{
    /// <summary>
    /// Время истечения токена (Unix timestamp)
    /// </summary>
    [JsonPropertyName("expired_at")]
    public long ExpiredAt { get; init; }

    /// <summary>
    /// Тип токена
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
