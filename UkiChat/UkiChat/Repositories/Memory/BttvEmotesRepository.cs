using System.Collections.Generic;
using UkiChat.Model.Bttv;

namespace UkiChat.Repositories.Memory;

public class BttvEmotesRepository : IBttvEmotesRepository
{
    // Dictionary<EmoteName, BttvEmote>
    private Dictionary<string, BttvEmote> _globalEmotes = new();

    // Dictionary<BroadcasterId, Dictionary<EmoteName, BttvEmote>>
    private readonly Dictionary<string, Dictionary<string, BttvEmote>> _channelEmotes = new();

    public Dictionary<string, BttvEmote> GetGlobalEmotes()
    {
        return _globalEmotes;
    }

    public Dictionary<string, BttvEmote> GetChannelEmotes(string broadcasterId)
    {
        return _channelEmotes.TryGetValue(broadcasterId, out var emotes)
            ? emotes
            : new Dictionary<string, BttvEmote>();
    }

    public void SetGlobalEmotes(List<BttvEmote> emotes)
    {
        var dict = new Dictionary<string, BttvEmote>();
        foreach (var emote in emotes) dict[emote.Name] = emote;
        _globalEmotes = dict;
    }

    public void SetChannelEmotes(string broadcasterId, List<BttvEmote> emotes)
    {
        var dict = new Dictionary<string, BttvEmote>();
        foreach (var emote in emotes) dict[emote.Name] = emote;
        _channelEmotes[broadcasterId] = dict;
    }

    public void Clear()
    {
        _globalEmotes.Clear();
        _channelEmotes.Clear();
    }
}
