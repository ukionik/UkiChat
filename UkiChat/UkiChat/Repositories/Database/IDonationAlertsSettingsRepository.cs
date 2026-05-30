using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public interface IDonationAlertsSettingsRepository
{
    DonationAlertsSettings GetActiveSettings();
    void Save(DonationAlertsSettings settings);
}
