using System.Text.Json.Serialization;

namespace UkiChat.Model.DonationAlerts;

/// <summary>
/// Ответ GET /api/v1/user/oauth — данные пользователя и токен для подключения к Centrifugo.
/// </summary>
public record DonationAlertsUserResponse
{
    [JsonPropertyName("data")]
    public DonationAlertsUserData Data { get; init; } = new();
}

public record DonationAlertsUserData
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("socket_connection_token")]
    public string SocketConnectionToken { get; init; } = string.Empty;
}
