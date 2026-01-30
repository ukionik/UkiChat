using System.Text.Json.Serialization;

namespace UkiChat.Model.VkVideoLive;

/// <summary>
/// Ответ от VK Video Live API при получении токена
/// </summary>
public record VkVideoLiveTokenResponse
{
    /// <summary>
    /// Токен, с помощью которого можно осуществлять запросы к DevAPI
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Значение переданное в параметре state при запросе окна авторизации
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>
    /// Время в секундах, после которого access_token перестанет действовать
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpireTime { get; init; }

    /// <summary>
    /// Тип токена. Всегда Bearer
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = string.Empty;
}
