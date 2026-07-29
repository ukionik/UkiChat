using UkiChat.Entities;

namespace UkiChat.Model.Twitch;

public record TwitchConnectionParams(
    string OldChannel,
    string NewChannel,
    string BroadcasterId,
    string ChatUsername,
    string ChatAccessToken
)
{
    /// <summary>
    ///     Учётные данные для IRC берутся из авторизации пользователя, а не из отдельного
    ///     бот-аккаунта: Twitch принимает любой user-токен со scope chat:read, а логин обязан
    ///     принадлежать владельцу этого токена. Пустые значения означают «не авторизован» —
    ///     ConnectAsync в этом случае подключение не начинает.
    /// </summary>
    public static TwitchConnectionParams OfTwitchSettings(string oldChannel,
        string newChannel,
        TwitchSettings twitchSettings)
    {
        return new TwitchConnectionParams(
            oldChannel,
            newChannel,
            twitchSettings.ApiBroadcasterId ?? "",
            twitchSettings.UserLogin ?? "",
            twitchSettings.UserAccessToken ?? ""
        );
    }
}