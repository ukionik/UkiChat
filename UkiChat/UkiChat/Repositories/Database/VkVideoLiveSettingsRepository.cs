using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public class VkVideoLiveSettingsRepository(LiteDatabase db) : IVkVideoLiveSettingsRepository
{
    private readonly ILiteCollection<VkVideoLiveSettings> _vkVideoLiveSettings = db.GetCollection<VkVideoLiveSettings>();

    public VkVideoLiveSettings GetActiveSettings()
    {
        return _vkVideoLiveSettings
            .Include(x => x.AppSettings)
            .Include(x => x.AppSettings.Profile)
            .FindOne(x => x.AppSettings.Profile.Active);
    }

    public void Save(VkVideoLiveSettings vkVideoLiveSettings)
    {
        _vkVideoLiveSettings.Upsert(vkVideoLiveSettings);
    }
}
