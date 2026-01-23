using UkiChat.Core;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public interface IAppSettingsRepository : IBaseCrudRepository<AppSettings, long>
{
    AppSettings GetActiveAppSettings();
}