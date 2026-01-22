using System.Threading.Tasks;
using TwitchLib.Api;
using UkiChat.Services;
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
    public async Task TwitchApiService_ValidateAccessTokenAsync_ReturnsTrue()
    {
        var appSettings = AppSettingsReader.Read();
        var service = new TwitchApiService();

        await service.InitializeAsync(appSettings.Twitch.Api.ClientId, appSettings.Twitch.Api.AccessToken);

        var isValid = await service.ValidateAccessTokenAsync();

        Assert.True(isValid);
    }

    [Fact]
    public async Task TwitchApiService_GetGlobalChatBadgesAsync_ReturnsBadges()
    {
        var appSettings = AppSettingsReader.Read();
        var service = new TwitchApiService();

        await service.InitializeAsync(appSettings.Twitch.Api.ClientId, appSettings.Twitch.Api.AccessToken);

        var result = await service.GetGlobalChatBadgesAsync();

        Assert.NotNull(result);
        Assert.NotEmpty(result.EmoteSet);
        testOutputHelper.WriteLine($"Found {result.EmoteSet.Length} badge sets");
    }

    [Fact]
    public async Task TwitchApiService_GetChannelChatBadgesAsync_ReturnsBadges()
    {
        var appSettings = AppSettingsReader.Read();
        var service = new TwitchApiService();

        await service.InitializeAsync(appSettings.Twitch.Api.ClientId, appSettings.Twitch.Api.AccessToken);

        // Twitch broadcaster ID (например, официальный канал Twitch)
        var broadcasterId = "12826";

        var result = await service.GetChannelChatBadgesAsync(broadcasterId);

        Assert.NotNull(result);
        testOutputHelper.WriteLine($"Found {result.EmoteSet.Length} channel badge sets");
    }

    [Fact]
    public async Task TwitchApiService_RefreshAccessTokenAsync_ReturnsNewTokens()
    {
        var appSettings = AppSettingsReader.Read();
        var service = new TwitchApiService();

        await service.InitializeAsync(appSettings.Twitch.Api.ClientId, appSettings.Twitch.Api.AccessToken);

        var result = await service.RefreshAccessTokenAsync(appSettings.Twitch.Api.RefreshToken, appSettings.Twitch.Api.ClientId, appSettings.Twitch.Api.ClientSecret);

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        testOutputHelper.WriteLine($"New AccessToken: {result.AccessToken}");
        testOutputHelper.WriteLine($"New RefreshToken: {result.RefreshToken}");
        testOutputHelper.WriteLine($"Expires in: {result.ExpiresIn} seconds");
    }

    [Fact]
    public async Task TwitchApiService_EnsureValidTokenAsync_ReturnsNullWhenTokenValid()
    {
        var appSettings = AppSettingsReader.Read();
        var service = new TwitchApiService();

        await service.InitializeAsync(appSettings.Twitch.Api.ClientId, appSettings.Twitch.Api.AccessToken);

        var result = await service.EnsureValidTokenAsync(appSettings.Twitch.Api.RefreshToken, appSettings.Twitch.Api.ClientId, appSettings.Twitch.Api.ClientSecret);

        // Если токен валиден, возвращается null
        Assert.Null(result);
        testOutputHelper.WriteLine("Token is valid, no refresh needed");
    }

    [Fact]
    public async Task TwitchApiService_EnsureValidTokenAsync_RefreshesWhenTokenInvalid()
    {
        var appSettings = AppSettingsReader.Read();
        var service = new TwitchApiService();

        // Инициализируем с невалидным токеном
        await service.InitializeAsync(
            appSettings.Twitch.Api.ClientId, "invalid_access_token");

        var result = await service.EnsureValidTokenAsync(appSettings.Twitch.Api.RefreshToken, appSettings.Twitch.Api.ClientId, appSettings.Twitch.Api.ClientSecret);

        // Токен был невалиден, должен быть обновлён
        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        testOutputHelper.WriteLine($"Token was refreshed. New AccessToken: {result.AccessToken}");
    }
}