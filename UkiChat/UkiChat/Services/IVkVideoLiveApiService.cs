using System.Threading.Tasks;
using UkiChat.Model.VkVideoLive;

namespace UkiChat.Services;

/// <summary>
/// Сервис для работы с VK Video Live API
/// </summary>
public interface IVkVideoLiveApiService
{
    /// <summary>
    /// Получить access token по client_id и client_secret
    /// </summary>
    /// <param name="clientId">Идентификатор приложения</param>
    /// <param name="clientSecret">Секретный ключ приложения</param>
    /// <returns>Ответ с access token</returns>
    Task<VkVideoLiveTokenResponse> GetAccessTokenAsync(string clientId, string clientSecret);

    /// <summary>
    /// Проверить валидность access token
    /// </summary>
    /// <param name="accessToken">Токен для проверки</param>
    /// <returns>Информация о токене</returns>
    Task<VkVideoLiveTokenInfoResponse> ValidateAccessTokenAsync(string accessToken);
}
