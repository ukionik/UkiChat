using UkiChat.Repositories;

namespace UkiChat.Configuration;

public interface IDatabaseContext
{
    ITwitchSettingsRepository TwitchSettingsRepository { get; }
    IProfileRepository ProfileRepository { get; }
}