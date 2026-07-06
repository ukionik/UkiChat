using System.Collections.Generic;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;
using UkiChat.Entities;

namespace UkiChat.Repositories.Memory;

/// <summary>
/// Репозиторий для хранения бейджей Twitch чата в памяти
/// </summary>
public interface ITwitchBadgesRepository
{
    /// <summary>
    /// Сохранить глобальные бейджи из ответа API
    /// </summary>
    void SetGlobalBadges(GetGlobalChatBadgesResponse badges);

    /// <summary>
    /// Сохранить глобальные бейджи из кэша в БД
    /// </summary>
    void SetGlobalBadges(IEnumerable<TwitchBadgeEntity> badges);

    /// <summary>
    /// Сохранить бейджи канала из ответа API
    /// </summary>
    /// <param name="broadcasterId">ID канала</param>
    /// <param name="badges">Ответ с бейджами</param>
    void SetChannelBadges(string broadcasterId, GetChannelChatBadgesResponse badges);

    /// <summary>
    /// Сохранить бейджи канала из кэша в БД
    /// </summary>
    /// <param name="broadcasterId">ID канала</param>
    /// <param name="badges">Кэшированные бейджи</param>
    void SetChannelBadges(string broadcasterId, IEnumerable<TwitchBadgeEntity> badges);

    /// <summary>
    /// Получить URL бейджей из коллекции badges
    /// </summary>
    /// <param name="badges">Коллекция бейджей из ChatMessage.Badges (Key=SetId, Value=Version)</param>
    /// <param name="broadcasterId">ID канала для поиска в channel badges</param>
    /// <returns>Список URL изображений бейджей</returns>
    List<string> GetBadgeUrls(ICollection<KeyValuePair<string, string>> badges, string broadcasterId);

    /// <summary>
    /// Очистить все бейджи
    /// </summary>
    void Clear();
}
