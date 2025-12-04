using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories;

public class TwitchSettingsRepository(LiteDatabase db) : ITwitchSettingsRepository
{
    private readonly ILiteCollection<TwitchSettings> _twitchSettings = db.GetCollection<TwitchSettings>();

    public TwitchSettings GetActiveSettings()
    {
        return _twitchSettings
            .Include(x => x.AppSettings)
            .Include(x => x.AppSettings.Profile)
            .FindOne(x => x.AppSettings.Profile.Active);
    }

    public void Save(TwitchSettings twitchGlobalSettings)
    {
        _twitchSettings.Upsert(twitchGlobalSettings);
    }
}