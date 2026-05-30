using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UkiChat.Model.DonationAlerts;

/// <summary>
/// Ответ POST /api/v1/centrifuge/subscribe — per-channel токены подписки на приватные каналы.
/// </summary>
public record DonationAlertsCentrifugeSubscribeResponse
{
    [JsonPropertyName("channels")]
    public List<DonationAlertsCentrifugeChannel> Channels { get; init; } = [];
}

public record DonationAlertsCentrifugeChannel
{
    [JsonPropertyName("channel")]
    public string Channel { get; init; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;
}
