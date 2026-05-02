using System.Collections.Generic;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public interface IBttvEmoteRepository
{
    List<BttvEmoteEntity> GetGlobalEmotes();
    List<BttvEmoteEntity> GetChannelEmotes(string broadcasterId);
    void SaveGlobalEmotes(IEnumerable<BttvEmoteEntity> emotes);
    void SaveChannelEmotes(string broadcasterId, IEnumerable<BttvEmoteEntity> emotes);
}
