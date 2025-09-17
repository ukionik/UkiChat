using LiteDB;

namespace UkiChat.Entities;

public class Profile : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }
    public required string Name { get; set; }
    public bool Active { get; set; }
    public bool Default { get; set; }
}