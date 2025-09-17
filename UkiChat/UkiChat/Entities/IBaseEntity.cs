using LiteDB;

namespace UkiChat.Entities;

public interface IBaseEntity<TId>
{
    [BsonId]
    public TId Id { get; set; }
}