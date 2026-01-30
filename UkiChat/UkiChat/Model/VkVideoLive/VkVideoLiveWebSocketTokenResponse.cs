using System.Text.Json.Serialization;

namespace UkiChat.Model.VkVideoLive;

/// <summary>
/// Ответ от VK Video Live API при получении токена для WebSocket
/// </summary>
public record VkVideoLiveWebSocketTokenResponse
{
    /// <summary>
    /// Данные ответа
    /// </summary>
    [JsonPropertyName("data")]
    public VkVideoLiveWebSocketTokenData Data { get; init; } = new();
}

/// <summary>
/// Данные токена для WebSocket
/// </summary>
public record VkVideoLiveWebSocketTokenData
{
    /// <summary>
    /// Токен для подключения к PubSub сервису
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;
}
