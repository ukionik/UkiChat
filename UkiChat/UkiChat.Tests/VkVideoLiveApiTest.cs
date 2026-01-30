using System;
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
            appSettings.VkVideoLive.Api.ClientId,
            appSettings.VkVideoLive.Api.ClientSecret);

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
            appSettings.VkVideoLive.Api.ClientId,
            appSettings.VkVideoLive.Api.ClientSecret);

        // Act
        var result = await service.ValidateAccessTokenAsync(tokenResponse.AccessToken);

        // Assert
        testOutputHelper.WriteLine($"Token Type: {result.Data.Type}");

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

    [Fact]
    public async Task VkVideoLiveApiService_GetChannelInfoAsync_ReturnsChannelInfo()
    {
        // Arrange
        var appSettings = AppSettingsReader.Read();
        var service = new VkVideoLiveApiService();

        // Act
        var result = await service.GetChannelInfoAsync(
            appSettings.VkVideoLive.Api.AccessToken,
            appSettings.VkVideoLive.Chat.Channel);

        // Assert
        testOutputHelper.WriteLine($"Channel ID: {result.Data.Channel.Id}");
        testOutputHelper.WriteLine($"Channel Nick: {result.Data.Channel.Nick}");
        testOutputHelper.WriteLine($"Channel URL: {result.Data.Channel.Url}");
        testOutputHelper.WriteLine($"Channel Status: {result.Data.Channel.Status}");
        testOutputHelper.WriteLine($"Owner ID: {result.Data.Owner.Id}");
        testOutputHelper.WriteLine($"Owner Nick: {result.Data.Owner.Nick}");
        testOutputHelper.WriteLine($"Subscribers: {result.Data.Channel.Counters?.Subscribers}");

        testOutputHelper.WriteLine("\nWebSocket Channels:");
        testOutputHelper.WriteLine($"  Chat: {result.Data.Channel.WebSocketChannels?.Chat}");
        testOutputHelper.WriteLine($"  Info: {result.Data.Channel.WebSocketChannels?.Info}");
        testOutputHelper.WriteLine($"  Channel Points: {result.Data.Channel.WebSocketChannels?.ChannelPoints}");
        testOutputHelper.WriteLine($"  Limited Chat: {result.Data.Channel.WebSocketChannels?.LimitedChat}");

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Channel);
        Assert.True(result.Data.Channel.Id > 0);
        Assert.NotEmpty(result.Data.Channel.Nick);
        Assert.NotEmpty(result.Data.Channel.Url);
        Assert.NotEmpty(result.Data.Channel.Status);

        Assert.NotNull(result.Data.Channel.WebSocketChannels);
        Assert.NotEmpty(result.Data.Channel.WebSocketChannels.Chat);
        Assert.NotEmpty(result.Data.Channel.WebSocketChannels.Info);
    }

    [Fact]
    public async Task VkVideoLiveApiService_GetChannelInfoAsync_WithInvalidChannel_ThrowsException()
    {
        // Arrange
        var appSettings = AppSettingsReader.Read();
        var service = new VkVideoLiveApiService();

        // Сначала получаем токен
        var tokenResponse = await service.GetAccessTokenAsync(
            appSettings.VkVideoLive.Api.ClientId,
            appSettings.VkVideoLive.Api.ClientSecret);

        // Act & Assert
        // Канал "invalid_channel_that_does_not_exist" должен вызвать ошибку
        await Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(async () =>
            await service.GetChannelInfoAsync(tokenResponse.AccessToken, "invalid_channel_that_does_not_exist"));

        testOutputHelper.WriteLine("Correctly threw exception for invalid channel");
    }

    [Fact]
    public async Task VkVideoLiveApiService_GetWebSocketTokenAsync_ReturnsToken()
    {
        // Arrange
        var appSettings = AppSettingsReader.Read();
        var service = new VkVideoLiveApiService();

        // Act
        var result = await service.GetWebSocketTokenAsync(appSettings.VkVideoLive.Api.AccessToken);

        // Assert
        testOutputHelper.WriteLine($"WebSocket Token: {result.Data.Token.Substring(0, Math.Min(20, result.Data.Token.Length))}...");

        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Token);
    }

    [Fact]
    public async Task VkVideoLiveApiService_GetWebSocketTokenAsync_WithInvalidToken_ThrowsException()
    {
        // Arrange
        var service = new VkVideoLiveApiService();

        // Act & Assert
        await Assert.ThrowsAsync<System.Net.Http.HttpRequestException>(async () =>
            await service.GetWebSocketTokenAsync("invalid_token"));
    }
}