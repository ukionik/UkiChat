using System.Text.Json.Serialization;

namespace UkiChat.Model.DonationAlerts;

/// <summary>
/// Полезная нагрузка push-события доната из канала Centrifugo $alerts:donation_{userId}.
/// </summary>
public record DonationAlertsDonation
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>Имя донатера (для анонимных — "Anonymous").</summary>
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    /// <summary>Текст сообщения доната.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>Тип сообщения: "text" или "audio".</summary>
    [JsonPropertyName("message_type")]
    public string? MessageType { get; init; }

    [JsonPropertyName("amount")]
    public double Amount { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }
}
