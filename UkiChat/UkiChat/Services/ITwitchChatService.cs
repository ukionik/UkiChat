using System.Threading.Tasks;
using UkiChat.Core;
using UkiChat.Model.Twitch;

namespace UkiChat.Services;

public interface ITwitchChatService : IChatService<TwitchConnectionParams>
{
    /// <summary>
    /// Перезагружает кастомные награды канала авторизованного пользователя.
    /// Вызывается после (раз)авторизации.
    /// </summary>
    Task ReloadCustomRewardsAsync();
}