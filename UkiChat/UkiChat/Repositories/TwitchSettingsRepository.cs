using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories;

public class TwitchSettingsRepository(LiteDatabase db) : ITwitchSettingsRepository
{
    private readonly ILiteCollection<TwitchSettings> _twitchSettings = db.GetCollection<TwitchSettings>();

    public TwitchSettings Get()
    {
        var settings = _twitchSettings.FindById(1);
        if (settings == null)
        {
            settings = new TwitchSettings();
            _twitchSettings.Upsert(settings);
        }

        return settings;
    }

    public void Save(TwitchSettings twitchGlobalSettings)
    {
        _twitchSettings.Upsert(twitchGlobalSettings);
    }
}