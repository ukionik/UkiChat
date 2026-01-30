using System.Threading.Tasks;
using UkiChat.Services;
using UkiChat.Tests.AppSettingsData;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class VkVideoLiveApiTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task VkVideoLiveApiService_GetAccessTokenAsync_ReturnsToken()
    {
        // Arrange
        var appSettings = AppSettingsReader.Read();
        var service = new VkVideoLiveApiService();

        // Act
        var result = await service.GetAccessTokenAsync(
            appSettings.VkVideoLiveApi.ClientId,
            appSettings.VkVideoLiveApi.ClientSecret);

        // Assert
        testOutputHelper.WriteLine($"Access Token: {result.AccessToken}");
        testOutputHelper.WriteLine($"Token Type: {result.TokenType}");
        testOutputHelper.WriteLine($"Expires in: {result.ExpireTime} seconds");

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpireTime > 0, $"ExpireTime должен быть больше 0, но был {result.ExpireTime}");
    }

    [Fact]
    public async Task VkVideoLiveApiService_GetAccessTokenAsync_WithInvalidCredentials_ThrowsException()
    {
        // Arrange
        var service = new VkVideoLiveApiService();

        // Act & Assert
        await Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(async () =>
            await service.GetAccessTokenAsync("invalid_client_id", "invalid_secret"));
    }

    [Fact]
    public async Task VkVideoLiveApiService_ValidateAccessTokenAsync_ReturnsTokenInfo()
    {
        // Arrange
        var appSettings = AppSettingsReader.Read();
        var service = new VkVideoLiveApiService();

        // Сначала получаем токен
        var tokenResponse = await service.GetAccessTokenAsync(
            appSettings.VkVideoLiveApi.ClientId,
            appSettings.VkVideoLiveApi.ClientSecret);

        // Act
        var result = await service.ValidateAccessTokenAsync(tokenResponse.AccessToken);

        // Assert
        testOutputHelper.WriteLine($"Token Type: {result.Data.Type}");
        testOutputHelper.WriteLine($"Expired At: {result.Data.ExpiredAt}");

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Type);
        Assert.True(result.Data.ExpiredAt > 0, $"ExpiredAt должен быть больше 0, но был {result.Data.ExpiredAt}");
    }

    [Fact]
    public async Task VkVideoLiveApiService_ValidateAccessTokenAsync_WithInvalidToken_ThrowsException()
    {
        // Arrange
        var service = new VkVideoLiveApiService();

        // Act & Assert
        await Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(async () =>
            await service.ValidateAccessTokenAsync("invalid_token"));
    }
}