using UkiChat.Model.DonationAlerts;

namespace UkiChat.Model.Chat.EventArgs;

public class DonationAlertsDonationEventArgs : System.EventArgs
{
    public DonationAlertsDonation? Donation { get; init; }
}
