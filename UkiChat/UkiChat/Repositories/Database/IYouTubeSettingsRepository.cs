using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public interface IYouTubeSettingsRepository
{
    YouTubeSettings GetActiveSettings();
    void Save(YouTubeSettings settings);
}
