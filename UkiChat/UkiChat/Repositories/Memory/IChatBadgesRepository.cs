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
    GetGlobalChatBadgesResponse? GetGlobalBadges();

    /// <summary>
    /// Получить бейджи канала
    /// </summary>
    /// <param name="broadcasterId">ID канала</param>
    GetChannelChatBadgesResponse? GetChannelBadges(string broadcasterId);

    /// <summary>
    /// Сохранить глобальные бейджи
    /// </summary>
    void SetGlobalBadges(GetGlobalChatBadgesResponse badges);

    /// <summary>
    /// Сохранить бейджи канала
    /// </summary>
    /// <param name="broadcasterId">ID канала</param>
    /// <param name="badges">Ответ с бейджами</param>
    void SetChannelBadges(string broadcasterId, GetChannelChatBadgesResponse badges);

    /// <summary>
    /// Очистить все бейджи
    /// </summary>
    void Clear();
}
