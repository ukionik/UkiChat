using System.Collections.Generic;
using UkiChat.Model.Ffz;

namespace UkiChat.Repositories.Memory;

public interface IFfzEmotesRepository
{
    Dictionary<string, FfzEmote> GetGlobalEmotes();
    Dictionary<string, FfzEmote> GetChannelEmotes(string broadcasterId);
    void SetGlobalEmotes(List<FfzEmote> emotes);
    void SetChannelEmotes(string broadcasterId, List<FfzEmote> emotes);
    void Clear();
}
