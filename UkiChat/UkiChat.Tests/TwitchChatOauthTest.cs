using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using UkiChat.Tests.AppSettingsData;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class TwitchChatOauthTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task ValidateTest()
    {
        try
        {
            var appSettigns = AppSettingsReader.Read();
            var accessToken = appSettigns.Twitch.Chat.AccessToken;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", accessToken);

            var response = await client.GetAsync("https://id.twitch.tv/oauth2/validate");

            if (!response.IsSuccessStatusCode)
            {
                testOutputHelper.WriteLine($"❌ Ошибка проверки токена: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                testOutputHelper.WriteLine(errorContent);
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var login = root.GetProperty("login").GetString();
            var expiresIn = root.GetProperty("expires_in").GetInt32();

            testOutputHelper.WriteLine($"✅ Токен принадлежит пользователю: {login}");
            testOutputHelper.WriteLine($"⏱ Время жизни токена: {expiresIn} секунд (~{expiresIn / 3600.0:F2} часов)");
        }
        catch (Exception ex)
        {
            testOutputHelper.WriteLine($"❌ Ошибка: {ex.Message}");
        }
    }
}