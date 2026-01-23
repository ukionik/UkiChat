using System.Collections.Generic;
using TwitchLib.Api.Helix.Models.Chat.Badges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;

namespace UkiChat.Repositories.Memory;

/// <summary>
/// Репозиторий для хранения бейджей Twitch чата в памяти
/// </summary>
public interface IChatBadgesRepository
{
    /// <summary>
    /// Получить глобальные бейджи
    /// </summary>
    /// <returns>Dictionary где ключ - SetId (например "subscriber"), значение - массив версий бейджа</returns>
    Dictionary<string, BadgeVersion[]> GetGlobalBadges();

    /// <summary>
    /// Получить бейджи канала
    /// </summary>
    /// <param name="broadcasterId">ID канала</param>
    /// <returns>Dictionary где ключ - SetId, значение - массив версий бейджа</returns>
    Dictionary<string, BadgeVersion[]> GetChannelBadges(string broadcasterId);

    /// <summary>
    /// Сохранить глобальные бейджи из ответа API
    /// </summary>
    void SetGlobalBadges(GetGlobalChatBadgesResponse badges);

    /// <summary>
    /// Сохранить бейджи канала из ответа API
    /// </summary>
    /// <param name="broadcasterId">ID канала</param>
    /// <param name="badges">Ответ с бейджами</param>
    void SetChannelBadges(string broadcasterId, GetChannelChatBadgesResponse badges);

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
