using System.Collections.Generic;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

/// <summary>
/// Кэш бейджей Twitch в БД — fallback на случай недоступности API при запуске
/// </summary>
public interface ITwitchBadgeRepository
{
    List<TwitchBadgeEntity> GetGlobalBadges();

    List<TwitchBadgeEntity> GetChannelBadges(string broadcasterId);

    void SaveGlobalBadges(IEnumerable<TwitchBadgeEntity> badges);

    void SaveChannelBadges(string broadcasterId, IEnumerable<TwitchBadgeEntity> badges);
}
