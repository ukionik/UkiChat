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

    /// <summary>
    /// Перечитывает учётные данные IRC из настроек и переподключает чат под ними.
    /// Вызывается после (раз)авторизации: чат подключается токеном пользователя.
    /// </summary>
    Task ReapplyCredentialsAsync();
}