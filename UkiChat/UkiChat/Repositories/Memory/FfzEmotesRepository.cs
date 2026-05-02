using System.Collections.Generic;
using UkiChat.Model.Ffz;

namespace UkiChat.Repositories.Memory;

public class FfzEmotesRepository : IFfzEmotesRepository
{
    // Dictionary<EmoteName, FfzEmote>
    private Dictionary<string, FfzEmote> _globalEmotes = new();

    // Dictionary<BroadcasterId, Dictionary<EmoteName, FfzEmote>>
    private readonly Dictionary<string, Dictionary<string, FfzEmote>> _channelEmotes = new();

    public Dictionary<string, FfzEmote> GetGlobalEmotes()
    {
        return _globalEmotes;
    }

    public Dictionary<string, FfzEmote> GetChannelEmotes(string broadcasterId)
    {
        return _channelEmotes.TryGetValue(broadcasterId, out var emotes)
            ? emotes
            : new Dictionary<string, FfzEmote>();
    }

    public void SetGlobalEmotes(List<FfzEmote> emotes)
    {
        var dict = new Dictionary<string, FfzEmote>();
        foreach (var emote in emotes) dict[emote.Name] = emote;
        _globalEmotes = dict;
    }

    public void SetChannelEmotes(string broadcasterId, List<FfzEmote> emotes)
    {
        var dict = new Dictionary<string, FfzEmote>();
        foreach (var emote in emotes) dict[emote.Name] = emote;
        _channelEmotes[broadcasterId] = dict;
    }

    public void Clear()
    {
        _globalEmotes.Clear();
        _channelEmotes.Clear();
    }
}
