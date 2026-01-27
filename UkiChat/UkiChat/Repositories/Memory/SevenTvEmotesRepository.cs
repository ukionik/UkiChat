using System.Collections.Generic;
using System.Linq;
using UkiChat.Model.SevenTv;

namespace UkiChat.Repositories.Memory;

/// <summary>
/// Репозиторий для хранения эмоутов 7TV в памяти
/// </summary>
public class SevenTvEmotesRepository : ISevenTvEmotesRepository
{
    // Dictionary<EmoteName, SevenTvEmote>
    private Dictionary<string, SevenTvEmote> _globalEmotes = new();

    // Dictionary<BroadcasterId, Dictionary<EmoteName, SevenTvEmote>>
    private readonly Dictionary<string, Dictionary<string, SevenTvEmote>> _channelEmotes = new();

    public Dictionary<string, SevenTvEmote> GetGlobalEmotes()
    {
        return _globalEmotes;
    }

    public Dictionary<string, SevenTvEmote> GetChannelEmotes(string broadcasterId)
    {
        return _channelEmotes.TryGetValue(broadcasterId, out var emotes)
            ? emotes
            : new Dictionary<string, SevenTvEmote>();
    }

    public void SetGlobalEmotes(List<SevenTvEmote> emotes)
    {
        _globalEmotes = emotes.ToDictionary(e => e.Name, e => e);
    }

    public void SetChannelEmotes(string broadcasterId, List<SevenTvEmote> emotes)
    {
        _channelEmotes[broadcasterId] = emotes.ToDictionary(e => e.Name, e => e);
    }

    public void Clear()
    {
        _globalEmotes.Clear();
        _channelEmotes.Clear();
    }
}
