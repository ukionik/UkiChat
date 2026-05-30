using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public class DonationAlertsSettingsRepository(LiteDatabase db) : IDonationAlertsSettingsRepository
{
    private readonly ILiteCollection<DonationAlertsSettings> _donationAlertsSettings =
        db.GetCollection<DonationAlertsSettings>();

    public DonationAlertsSettings GetActiveSettings()
    {
        return _donationAlertsSettings
            .Include(x => x.AppSettings)
            .Include(x => x.AppSettings.Profile)
            .FindOne(x => x.AppSettings.Profile.Active);
    }

    public void Save(DonationAlertsSettings settings)
    {
        _donationAlertsSettings.Upsert(settings);
    }
}
