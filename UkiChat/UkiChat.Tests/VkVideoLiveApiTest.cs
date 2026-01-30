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
}