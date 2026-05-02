using LiteDB;

namespace UkiChat.Entities;

public class FfzEmoteEntity : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }

    public string EmoteId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// null — глобальный эмоут, broadcasterId — эмоут канала
    /// </summary>
    public string? Channel { get; set; }
}
