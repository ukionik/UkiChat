using UkiChat.Repositories;

namespace UkiChat.Configuration;

public interface IDatabaseContext
{
    IAppSettingsRepository AppSettingsRepository { get; }
    ITwitchSettingsRepository TwitchSettingsRepository { get; }
    IProfileRepository ProfileRepository { get; }
}