using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public interface ITwitchSettingsRepository
{
    TwitchSettings GetActiveSettings();
    void Save(TwitchSettings settings);
}