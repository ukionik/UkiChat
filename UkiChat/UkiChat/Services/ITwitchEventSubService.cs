using System.Threading.Tasks;

namespace UkiChat.Services;

public interface ITwitchEventSubService
{
    /// <summary>
    /// Подключает EventSub WebSocket и подписывается на активации наград текущего
    /// авторизованного пользователя. No-op, если пользователь не авторизован или уже запущено.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Отключает EventSub WebSocket.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Перезапускает соединение (после смены авторизации).
    /// </summary>
    Task RestartAsync();
}
