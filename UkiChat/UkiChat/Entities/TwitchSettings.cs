using LiteDB;

namespace UkiChat.Entities;

public class TwitchSettings : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }

    // Chat credentials
    public required string ChatbotUsername { get; set; }
    public required string ChatbotAccessToken { get; set; }
    public required string Channel { get; set; }

    // API credentials
    public required string ApiClientId { get; set; }
    public required string ApiClientSecret { get; set; }
    public required string ApiAccessToken { get; set; }
    public required string ApiRefreshToken { get; set; }

    [BsonRef]
    public required AppSettings AppSettings { get; set; }
}