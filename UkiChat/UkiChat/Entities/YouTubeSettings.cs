using LiteDB;

namespace UkiChat.Entities;

public class YouTubeSettings : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }

    // Канал YouTube: @handle, channelId (UC...) или URL. InnerTube не требует токенов.
    public string? Channel { get; set; }

    [BsonRef]
    public AppSettings? AppSettings { get; set; }
}
