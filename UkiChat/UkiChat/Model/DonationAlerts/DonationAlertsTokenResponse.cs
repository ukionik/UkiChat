using System.Text.Json.Serialization;

namespace UkiChat.Model.DonationAlerts;

/// <summary>
/// Ответ DonationAlerts OAuth при обмене кода / обновлении токена.
/// </summary>
public record DonationAlertsTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = string.Empty;
}
