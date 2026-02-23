using System.Collections.Generic;
using System.Linq;
using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public class SevenTvEmoteRepository(LiteDatabase db) : ISevenTvEmoteRepository
{
    private readonly ILiteCollection<SevenTvEmoteEntity> _emotes =
        db.GetCollection<SevenTvEmoteEntity>("seven_tv_emotes");

    public List<SevenTvEmoteEntity> GetGlobalEmotes()
    {
        return _emotes.Find(x => x.Channel == null).ToList();
    }

    public List<SevenTvEmoteEntity> GetChannelEmotes(string broadcasterId)
    {
        return _emotes.Find(x => x.Channel == broadcasterId).ToList();
    }

    public void SaveGlobalEmotes(IEnumerable<SevenTvEmoteEntity> emotes)
    {
        _emotes.DeleteMany(x => x.Channel == null);
        _emotes.InsertBulk(emotes);
    }

    public void SaveChannelEmotes(string broadcasterId, IEnumerable<SevenTvEmoteEntity> emotes)
    {
        _emotes.DeleteMany(x => x.Channel == broadcasterId);
        _emotes.InsertBulk(emotes);
    }
}
