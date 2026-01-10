using System.Threading.Tasks;
using TwitchLib.Api;
using UkiChat.Tests.AppSettingsData;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class TwitchApiTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task AuthTest()
    {
        //Сначала получаем код по ссылке https://id.twitch.tv/oauth2/authorize?response_type=code&client_id=<code>&redirect_uri=http://localhost
        //Затем по коду получаем AccessToken и RefreshToken
        var appSettings = AppSettingsReader.Read();
        var api = new TwitchAPI();
        var res = await api.Auth.GetAccessTokenFromCodeAsync(appSettings.Twitch.Api.Code
            , appSettings.Twitch.Api.ClientSecret, "http://localhost"
            , appSettings.Twitch.Api.ClientId);
        
    }

    [Fact]
    public async Task GlobalBadgesAsyncTest()
    {
        var appSettings = AppSettingsReader.Read();
        var api = new TwitchAPI
        {
            Settings =
            {
                ClientId = appSettings.Twitch.Api.ClientId,
                AccessToken = appSettings.Twitch.Api.AccessToken
            }
        };
        var validationResult = await api.Auth.ValidateAccessTokenAsync();
        Assert.NotNull(validationResult);
        testOutputHelper.WriteLine(validationResult.ExpiresIn + " seconds");
        var res = await api.Helix.Chat.GetGlobalChatBadgesAsync();
        testOutputHelper.WriteLine(res.ToString());
    }
}