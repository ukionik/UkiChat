using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories;

public class TwitchGlobalSettingsRepository(LiteDatabase db) : ITwitchGlobalSettingsRepository
{
    private readonly ILiteCollection<TwitchGlobalSettings> _twitchGlobalSettings = db.GetCollection<TwitchGlobalSettings>();

    public TwitchGlobalSettings Get()
    {
        var settings = _twitchGlobalSettings.FindById(1);
        if (settings == null)
        {
            settings = new TwitchGlobalSettings();
            _twitchGlobalSettings.Upsert(settings);
        }
        return settings;
    }

    public void Save(TwitchGlobalSettings twitchGlobalSettings)
    {
        _twitchGlobalSettings.Upsert(twitchGlobalSettings);
    }
}