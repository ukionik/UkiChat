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
        // Channel проставляем здесь: вызывающий код передаёт broadcasterId отдельным аргументом и
        // пометку не ставил, поэтому эмоты канала уезжали в базу как глобальные (Channel == null).
        // Последствий было три: кэш канала в базе всегда пустой (fallback при недоступном API не
        // работал), эмоты канала подмешивались в глобальные, а DeleteMany ниже ничего не удалял —
        // записи копились от запуска к запуску.
        var channelEmotes = emotes.ToList();
        foreach (var emote in channelEmotes)
            emote.Channel = broadcasterId;

        _emotes.DeleteMany(x => x.Channel == broadcasterId);
        _emotes.InsertBulk(channelEmotes);
    }
}
