using LiteDB;

namespace UkiChat.Entities;

public class VkVideoLiveSettings : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }

    public string Channel { get; set; }

    // API credentials
    public string? ApiClientId { get; set; }
    public string? ApiClientSecret { get; set; }
    public string? ApiAccessToken { get; set; }

    [BsonRef]
    public AppSettings AppSettings { get; set; }
}
