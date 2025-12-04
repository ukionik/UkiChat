using LiteDB;

namespace UkiChat.Entities;

public class AppSettings : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }
    public required string Language { get; set; }
    [BsonRef]
    public Profile Profile { get; set; }
}