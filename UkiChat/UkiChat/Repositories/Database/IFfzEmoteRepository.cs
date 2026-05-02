using System.Collections.Generic;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public interface IFfzEmoteRepository
{
    List<FfzEmoteEntity> GetGlobalEmotes();
    List<FfzEmoteEntity> GetChannelEmotes(string broadcasterId);
    void SaveGlobalEmotes(IEnumerable<FfzEmoteEntity> emotes);
    void SaveChannelEmotes(string broadcasterId, IEnumerable<FfzEmoteEntity> emotes);
}
