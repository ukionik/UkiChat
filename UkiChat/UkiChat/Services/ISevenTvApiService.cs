using System.Collections.Generic;
using System.Threading.Tasks;
using UkiChat.Model.SevenTv;

namespace UkiChat.Services;

/// <summary>
/// Сервис для работы с 7TV API
/// </summary>
public interface ISevenTvApiService
{
    /// <summary>
    /// Получить глобальные эмоуты 7TV
    /// </summary>
    Task<List<SevenTvEmote>> GetGlobalEmotesAsync();

    /// <summary>
    /// Получить эмоуты канала по Twitch broadcaster ID
    /// </summary>
    Task<List<SevenTvEmote>> GetChannelEmotesAsync(string twitchBroadcasterId);
}
