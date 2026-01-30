using System;
using System.Threading.Tasks;
using UkiChat.Services;
using UkiChat.Tests.AppSettingsData;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class VkVideoLiveChatTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task VkVideoLiveChatService_ConnectAsync_CanConnect()
    {
        // Arrange
        var appSettings = AppSettingsReader.Read();
        var apiService = new VkVideoLiveApiService();
        var chatService = new VkVideoLiveChatService(apiService);

        var connectedEvent = new TaskCompletionSource<bool>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();

        chatService.Connected += (sender, e) =>
        {
            testOutputHelper.WriteLine("Connected to VK Video Live chat!");
            connectedEvent.TrySetResult(true);
        };

        chatService.MessageReceived += (sender, e) =>
        {
            testOutputHelper.WriteLine($"Message received from channel '{e.Channel}': {e.Data}");
            messageReceivedEvent.TrySetResult(true);
        };

        chatService.Error += (sender, e) =>
        {
            testOutputHelper.WriteLine($"Error: {e.Message}");
            if (e.Exception != null)
            {
                testOutputHelper.WriteLine($"Exception: {e.Exception}");
            }
        };

        chatService.Disconnected += (sender, e) =>
        {
            testOutputHelper.WriteLine($"Disconnected: {e.Reason}");
        };

        try
        {
            // Получаем информацию о канале
            var channelInfo = await apiService.GetChannelInfoAsync(
                appSettings.VkVideoLive.Api.AccessToken,
                appSettings.VkVideoLive.Chat.Channel);

            testOutputHelper.WriteLine($"Channel ID: {channelInfo.Data.Channel.Id}");
            testOutputHelper.WriteLine($"Channel Nick: {channelInfo.Data.Channel.Nick}");
            testOutputHelper.WriteLine($"Channel Status: {channelInfo.Data.Channel.Status}");
            testOutputHelper.WriteLine($"Chat WebSocket Channel: {channelInfo.Data.Channel.WebSocketChannels?.Chat}");

            Assert.NotNull(channelInfo.Data.Channel.WebSocketChannels);
            Assert.NotEmpty(channelInfo.Data.Channel.WebSocketChannels.Chat);

            // Act - Подключаемся к чату
            testOutputHelper.WriteLine("\nConnecting to chat WebSocket...");
            await chatService.ConnectAsync(
                appSettings.VkVideoLive.Api.AccessToken,
                channelInfo.Data.Channel.WebSocketChannels.Chat);

            // Ждем подключения (максимум 10 секунд)
            var connected = await Task.WhenAny(
                connectedEvent.Task,
                Task.Delay(TimeSpan.FromSeconds(10)));

            // Assert
            if (connected == connectedEvent.Task && connectedEvent.Task.IsCompletedSuccessfully)
            {
                testOutputHelper.WriteLine("Successfully connected!");

                // Ждем сообщение максимум 30 секунд
                testOutputHelper.WriteLine("Waiting for messages (30 seconds)...");
                var messageTask = await Task.WhenAny(
                    messageReceivedEvent.Task,
                    Task.Delay(TimeSpan.FromSeconds(30)));

                if (messageTask == messageReceivedEvent.Task && messageReceivedEvent.Task.IsCompletedSuccessfully)
                {
                    testOutputHelper.WriteLine("Message received!");
                }
                else
                {
                    testOutputHelper.WriteLine("No messages received in 30 seconds (this is OK if chat is inactive)");
                }

                Assert.True(true, "Connection successful");
            }
            else
            {
                Assert.Fail("Connection timeout - failed to connect in 10 seconds");
            }
        }
        finally
        {
            // Отключаемся
            testOutputHelper.WriteLine("\nDisconnecting...");
            await chatService.DisconnectAsync();
            chatService.Dispose();
        }
    }

    [Fact]
    public async Task VkVideoLiveChatService_ConnectAsync_WithInvalidToken_ThrowsException()
    {
        // Arrange
        var appSettings = AppSettingsReader.Read();
        var apiService = new VkVideoLiveApiService();
        var chatService = new VkVideoLiveChatService(apiService);

        // Получаем информацию о канале
        var channelInfo = await apiService.GetChannelInfoAsync(
            appSettings.VkVideoLive.Api.AccessToken,
            appSettings.VkVideoLive.Chat.Channel);

        try
        {
            // Act & Assert - Подключаемся с неверным токеном
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await chatService.ConnectAsync(
                    "invalid_token",
                    channelInfo.Data.Channel.WebSocketChannels!.Chat);
            });

            testOutputHelper.WriteLine("Correctly threw exception for invalid token");
        }
        finally
        {
            chatService.Dispose();
        }
    }
}
