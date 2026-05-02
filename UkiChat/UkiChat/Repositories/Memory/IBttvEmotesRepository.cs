using System.Collections.Generic;
using UkiChat.Model.Bttv;

namespace UkiChat.Repositories.Memory;

public interface IBttvEmotesRepository
{
    Dictionary<string, BttvEmote> GetGlobalEmotes();
    Dictionary<string, BttvEmote> GetChannelEmotes(string broadcasterId);
    void SetGlobalEmotes(List<BttvEmote> emotes);
    void SetChannelEmotes(string broadcasterId, List<BttvEmote> emotes);
    void Clear();
}
