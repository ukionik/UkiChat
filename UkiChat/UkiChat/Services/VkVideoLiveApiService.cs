using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UkiChat.Model.VkVideoLive;

namespace UkiChat.Services;

/// <summary>
///     Сервис для работы с VK Video Live API
/// </summary>
public class VkVideoLiveApiService : IVkVideoLiveApiService
{
    private const string AuthUrl = "https://api.live.vkvideo.ru";
    private const string BaseUrl = "https://apidev.live.vkvideo.ru";
    private readonly HttpClient _httpClient;

    public VkVideoLiveApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<VkVideoLiveTokenResponse> GetAccessTokenAsync(string clientId, string clientSecret)
    {
        // Формируем base64(client_id:secret)
        var credentials = $"{clientId}:{clientSecret}";
        var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        // Создаем HTTP запрос
        var requestUri = AuthUrl + "/oauth/server/token";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

        // Добавляем заголовки
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

        // Формируем тело запроса
        var formContent = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        ]);

        formContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        request.Content = formContent;

        // Отправляем запрос
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // Парсим ответ
        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<VkVideoLiveTokenResponse>(json);

        if (tokenResponse == null)
            throw new InvalidOperationException("Failed to deserialize token response");

        return tokenResponse;
    }

    public async Task<VkVideoLiveTokenInfoResponse> ValidateAccessTokenAsync(string accessToken)
    {
        // Создаем HTTP запрос
        var requestUri = BaseUrl + "/v1/token";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        // Добавляем Bearer авторизацию
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Отправляем запрос
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // Парсим ответ
        var json = await response.Content.ReadAsStringAsync();
        var tokenInfoResponse = JsonSerializer.Deserialize<VkVideoLiveTokenInfoResponse>(json);

        if (tokenInfoResponse == null)
            throw new InvalidOperationException("Failed to deserialize token info response");

        return tokenInfoResponse;
    }
}