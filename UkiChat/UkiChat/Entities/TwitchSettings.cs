using LiteDB;

namespace UkiChat.Entities;

public class TwitchSettings : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }
    
    public string ChatbotUsername { get; set; }
    public string ChatbotAccessToken { get; set; }
    public string Channel { get; set; }
    
    [BsonRef]
    public AppSettings AppSettings { get; set; }
}