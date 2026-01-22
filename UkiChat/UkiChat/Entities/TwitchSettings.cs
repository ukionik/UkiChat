using LiteDB;

namespace UkiChat.Entities;

public class TwitchSettings : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }

    // Chat credentials
    public string ChatbotUsername { get; set; }
    public string ChatbotAccessToken { get; set; }
    public string Channel { get; set; }

    // API credentials
    public string? ApiClientId { get; set; }
    public string? ApiClientSecret { get; set; }
    public string? ApiAccessToken { get; set; }
    public string? ApiRefreshToken { get; set; }

    [BsonRef]
    public AppSettings AppSettings { get; set; }
}