using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public class YouTubeSettingsRepository(LiteDatabase db) : IYouTubeSettingsRepository
{
    private readonly ILiteCollection<YouTubeSettings> _youTubeSettings = db.GetCollection<YouTubeSettings>();

    public YouTubeSettings GetActiveSettings()
    {
        return _youTubeSettings
            .Include(x => x.AppSettings)
            .Include(x => x.AppSettings.Profile)
            .FindOne(x => x.AppSettings.Profile.Active);
    }

    public void Save(YouTubeSettings youTubeSettings)
    {
        _youTubeSettings.Upsert(youTubeSettings);
    }
}
