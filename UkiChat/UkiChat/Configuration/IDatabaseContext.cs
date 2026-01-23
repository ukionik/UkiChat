using UkiChat.Repositories;
using UkiChat.Repositories.Database;

namespace UkiChat.Configuration;

public interface IDatabaseContext
{
    IAppSettingsRepository AppSettingsRepository { get; }
    ITwitchSettingsRepository TwitchSettingsRepository { get; }
    IProfileRepository ProfileRepository { get; }
}