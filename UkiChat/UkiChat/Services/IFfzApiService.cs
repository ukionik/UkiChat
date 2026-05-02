using System.Collections.Generic;
using System.Threading.Tasks;
using UkiChat.Model.Ffz;

namespace UkiChat.Services;

public interface IFfzApiService
{
    Task<List<FfzEmote>> GetGlobalEmotesAsync();
    Task<List<FfzEmote>> GetChannelEmotesAsync(string twitchBroadcasterId);
}
