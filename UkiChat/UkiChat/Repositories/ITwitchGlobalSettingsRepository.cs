using UkiChat.Entities;

namespace UkiChat.Repositories;

public interface ITwitchGlobalSettingsRepository
{
    TwitchGlobalSettings Get();
    void Save(TwitchGlobalSettings settings);
}