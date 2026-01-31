using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public interface IVkVideoLiveSettingsRepository
{
    VkVideoLiveSettings GetActiveSettings();
    void Save(VkVideoLiveSettings settings);
}
