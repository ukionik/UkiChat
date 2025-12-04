using UkiChat.Entities;

namespace UkiChat.Repositories;

public interface ITwitchSettingsRepository
{
    TwitchSettings GetActiveSettings();
    void Save(TwitchSettings settings);
}