using System.Collections.Generic;
using System.Threading.Tasks;
using UkiChat.Model.Bttv;

namespace UkiChat.Services;

public interface IBttvApiService
{
    Task<List<BttvEmote>> GetGlobalEmotesAsync();
    Task<List<BttvEmote>> GetChannelEmotesAsync(string twitchBroadcasterId);
}
