using LiteDB;

namespace UkiChat.Entities;

/// <summary>
/// Версия бейджа Twitch, хранящаяся в базе данных как кэш на случай недоступности API при запуске.
/// </summary>
public class TwitchBadgeEntity : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }

    /// <summary>
    /// Идентификатор набора бейджей (например "subscriber", "moderator")
    /// </summary>
    public string SetId { get; set; }

    /// <summary>
    /// Идентификатор версии внутри набора
    /// </summary>
    public string VersionId { get; set; }

    public string ImageUrl { get; set; }

    /// <summary>
    /// null — глобальный бейдж, broadcasterId — бейдж канала
    /// </summary>
    public string? Channel { get; set; }
}
