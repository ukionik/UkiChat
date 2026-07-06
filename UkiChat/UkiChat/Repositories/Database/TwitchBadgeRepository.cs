using System.Collections.Generic;
using System.Linq;
using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public class TwitchBadgeRepository(LiteDatabase db) : ITwitchBadgeRepository
{
    private readonly ILiteCollection<TwitchBadgeEntity> _badges =
        db.GetCollection<TwitchBadgeEntity>("twitch_badges");

    public List<TwitchBadgeEntity> GetGlobalBadges()
    {
        return _badges.Find(x => x.Channel == null).ToList();
    }

    public List<TwitchBadgeEntity> GetChannelBadges(string broadcasterId)
    {
        return _badges.Find(x => x.Channel == broadcasterId).ToList();
    }

    public void SaveGlobalBadges(IEnumerable<TwitchBadgeEntity> badges)
    {
        _badges.DeleteMany(x => x.Channel == null);
        _badges.InsertBulk(badges);
    }

    public void SaveChannelBadges(string broadcasterId, IEnumerable<TwitchBadgeEntity> badges)
    {
        _badges.DeleteMany(x => x.Channel == broadcasterId);
        _badges.InsertBulk(badges);
    }
}
