using System.Collections.Generic;
using UkiChat.Model.SevenTv;

namespace UkiChat.Repositories.Memory;

/// <summary>
/// Репозиторий для хранения эмоутов 7TV в памяти
/// </summary>
public interface ISevenTvEmotesRepository
{
    /// <summary>
    /// Получить глобальные эмоуты 7TV
    /// </summary>
    Dictionary<string, SevenTvEmote> GetGlobalEmotes();

    /// <summary>
    /// Получить эмоуты канала 7TV
    /// </summary>
    /// <param name="broadcasterId">ID канала Twitch</param>
    Dictionary<string, SevenTvEmote> GetChannelEmotes(string broadcasterId);

    /// <summary>
    /// Сохранить глобальные эмоуты
    /// </summary>
    void SetGlobalEmotes(List<SevenTvEmote> emotes);

    /// <summary>
    /// Сохранить эмоуты канала
    /// </summary>
    void SetChannelEmotes(string broadcasterId, List<SevenTvEmote> emotes);

    /// <summary>
    /// Очистить все эмоуты
    /// </summary>
    void Clear();
}
