using System.Collections.Generic;
using System.Linq;
using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public class FfzEmoteRepository(LiteDatabase db) : IFfzEmoteRepository
{
    private readonly ILiteCollection<FfzEmoteEntity> _emotes =
        db.GetCollection<FfzEmoteEntity>("ffz_emotes");

    public List<FfzEmoteEntity> GetGlobalEmotes()
    {
        return _emotes.Find(x => x.Channel == null).ToList();
    }

    public List<FfzEmoteEntity> GetChannelEmotes(string broadcasterId)
    {
        return _emotes.Find(x => x.Channel == broadcasterId).ToList();
    }

    public void SaveGlobalEmotes(IEnumerable<FfzEmoteEntity> emotes)
    {
        _emotes.DeleteMany(x => x.Channel == null);
        _emotes.InsertBulk(emotes);
    }

    public void SaveChannelEmotes(string broadcasterId, IEnumerable<FfzEmoteEntity> emotes)
    {
        _emotes.DeleteMany(x => x.Channel == broadcasterId);
        _emotes.InsertBulk(emotes);
    }
}
