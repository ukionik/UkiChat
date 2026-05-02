using System.Collections.Generic;
using System.Linq;
using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public class BttvEmoteRepository(LiteDatabase db) : IBttvEmoteRepository
{
    private readonly ILiteCollection<BttvEmoteEntity> _emotes =
        db.GetCollection<BttvEmoteEntity>("bttv_emotes");

    public List<BttvEmoteEntity> GetGlobalEmotes()
    {
        return _emotes.Find(x => x.Channel == null).ToList();
    }

    public List<BttvEmoteEntity> GetChannelEmotes(string broadcasterId)
    {
        return _emotes.Find(x => x.Channel == broadcasterId).ToList();
    }

    public void SaveGlobalEmotes(IEnumerable<BttvEmoteEntity> emotes)
    {
        _emotes.DeleteMany(x => x.Channel == null);
        _emotes.InsertBulk(emotes);
    }

    public void SaveChannelEmotes(string broadcasterId, IEnumerable<BttvEmoteEntity> emotes)
    {
        _emotes.DeleteMany(x => x.Channel == broadcasterId);
        _emotes.InsertBulk(emotes);
    }
}
