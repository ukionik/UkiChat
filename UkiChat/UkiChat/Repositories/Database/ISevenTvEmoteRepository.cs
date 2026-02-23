using System.Collections.Generic;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public interface ISevenTvEmoteRepository
{
    List<SevenTvEmoteEntity> GetGlobalEmotes();
    List<SevenTvEmoteEntity> GetChannelEmotes(string broadcasterId);
    void SaveGlobalEmotes(IEnumerable<SevenTvEmoteEntity> emotes);
    void SaveChannelEmotes(string broadcasterId, IEnumerable<SevenTvEmoteEntity> emotes);
}
