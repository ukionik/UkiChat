using UkiChat.Entities;

namespace UkiChat.Repositories;

public interface ITwitchSettingsRepository
{
    TwitchSettings Get();
    void Save(TwitchSettings settings);
}