using LiteDB;

namespace UkiChat.Entities;

/// <summary>
/// Эмоут 7TV, хранящийся в базе данных.
/// Id = "global:{emoteId}" для глобальных, "{broadcasterId}:{emoteId}" для канальных.
/// </summary>
public class SevenTvEmoteEntity
{
    [BsonId]
    public string Id { get; set; } = "";

    public string EmoteId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";

    /// <summary>
    /// null — глобальный эмоут, broadcasterId — эмоут канала
    /// </summary>
    public string? Channel { get; set; }
}
