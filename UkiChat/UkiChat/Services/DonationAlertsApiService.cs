using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using UkiChat.Diagnostics;
using UkiChat.Model.DonationAlerts;

namespace UkiChat.Services;

/// <summary>
///     HTTP-клиент DonationAlerts: OAuth-токены, данные пользователя, токены подписки Centrifugo.
/// </summary>
public class DonationAlertsApiService : IDonationAlertsApiService
{
    private const string OAuthTokenUrl = "https://www.donationalerts.com/oauth/token";
    private const string UserUrl = "https://www.donationalerts.com/api/v1/user/oauth";
    private const string CentrifugeSubscribeUrl = "https://www.donationalerts.com/api/v1/centrifuge/subscribe";
    private const string Scopes = "oauth-user-show oauth-donation-subscribe";

    private readonly HttpClient _httpClient;

    public DonationAlertsApiService()
    {
        _httpClient = new HttpClient(new DiagnosticHttpHandler("da"))
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<DonationAlertsTokenResponse> ExchangeCodeForTokensAsync(string code, string clientId, string clientSecret, string redirectUri)
    {
        var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("code", code)
        ]);

        var response = await _httpClient.PostAsync(OAuthTokenUrl, form);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DonationAlertsTokenResponse>(json)
               ?? throw new InvalidOperationException("Failed to deserialize DonationAlerts token response");
    }

    public async Task<DonationAlertsTokenResponse> RefreshTokensAsync(string refreshToken, string clientId, string clientSecret)
    {
        var form = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("scope", Scopes)
        ]);

        var response = await _httpClient.PostAsync(OAuthTokenUrl, form);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DonationAlertsTokenResponse>(json)
               ?? throw new InvalidOperationException("Failed to deserialize DonationAlerts token response");
    }

    public async Task<DonationAlertsUserResponse> GetUserAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, UserUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DonationAlertsUserResponse>(json)
               ?? throw new InvalidOperationException("Failed to deserialize DonationAlerts user response");
    }

    public async Task<string?> GetCentrifugeSubscribeTokenAsync(string accessToken, string clientId, string channel)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, CentrifugeSubscribeUrl)
        {
            Content = JsonContent.Create(new { channels = new[] { channel }, client = clientId })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var parsed = JsonSerializer.Deserialize<DonationAlertsCentrifugeSubscribeResponse>(json);

        // Ищем токен именно для запрошенного канала; обычно один элемент.
        var match = parsed?.Channels.Find(c => c.Channel == channel) ??
                    (parsed?.Channels.Count > 0 ? parsed.Channels[0] : null);
        return match?.Token;
    }
}
