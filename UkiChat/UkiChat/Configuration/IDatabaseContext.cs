using UkiChat.Repositories;

namespace UkiChat.Configuration;

public interface IDatabaseContext
{
    ITwitchGlobalSettingsRepository TwitchGlobalSettingsRepository { get; }
    IProfileRepository ProfileRepository { get; }
}