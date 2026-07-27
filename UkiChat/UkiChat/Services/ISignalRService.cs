using System.Threading.Tasks;
using UkiChat.Model.Chat;
using UkiChat.Model.Settings;

namespace UkiChat.Services;

public interface ISignalRService
{
    /// <summary>Заводит очередь отправки для подключившегося клиента (вызывается из AppHub).</summary>
    void RegisterConnection(string connectionId);

    /// <summary>Закрывает очередь отключившегося клиента (вызывается из AppHub).</summary>
    void UnregisterConnection(string connectionId);

    Task SendChatMessageAsync(UkiChatMessage message);
    Task SendMessageDeletedAsync(string messageId);
    Task SendUserMessagesDeletedAsync(string username);
    Task SendTwitchReconnect();
    Task SendVkVideoLiveReconnect();
    Task SendYouTubeReconnect();
    Task SendTwitchAuthChanged(TwitchAuthStatusData status);
    Task SendDonationAlertsAuthChanged(DonationAlertsAuthStatusData status);
}