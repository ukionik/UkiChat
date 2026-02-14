using System;
using System.Threading.Tasks;
using UkiChat.Model.VkVideoLive;
using UkiChat.Services;
using UkiChat.Tests.AppSettingsData;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class VkVideoLiveChatClientTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task VkVideoLiveChatService_ConnectAsync_CanConnect()
    {
        // Arrange
        var appSettings = AppSettingsReader.Read();
        var apiService = new VkVideoLiveApiService();
        var chatClient = new VkVideoLiveChatClient();

        var connectedEvent = new TaskCompletionSource<bool>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();

        chatClient.Connected += (sender, e) =>
        {
            testOutputHelper.WriteLine("Connected to VK Video Live chat!");
            connectedEvent.TrySetResult(true);
        };

        chatClient.MessageReceived += (sender, e) =>
        {
            var author = e.Message?.Data?.Author?.DisplayName ?? "Unknown";
            testOutputHelper.WriteLine($"Message received from channel '{e.Channel}' by '{author}'");
            messageReceivedEvent.TrySetResult(true);
        };

        chatClient.Error += (sender, e) =>
        {
            testOutputHelper.WriteLine($"Error: {e.Message}");
            if (e.Exception != null)
            {
                testOutputHelper.WriteLine($"Exception: {e.Exception}");
            }
        };

        chatClient.Disconnected += (sender, e) =>
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
            
            //Получаем WsToken
            var tokenResponse = await  apiService.GetWebSocketTokenAsync(appSettings.VkVideoLive.Api.AccessToken);
            var wsTokne = tokenResponse.Data.Token;
            testOutputHelper.WriteLine($"WS Token: {wsTokne}");

            // Act - Подключаемся к чату
            testOutputHelper.WriteLine("Connecting to chat WebSocket...");
            await chatClient.ConnectAsync(tokenResponse.Data.Token, "channel-" + channelInfo.Data.Channel.Id);

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
            await chatClient.DisconnectAsync();
            chatClient.Dispose();
        }
    }

    [Fact]
    public async Task VkVideoLiveChatService_ConnectAsync_WithInvalidToken_ThrowsException()
    {
        // Arrange
        var appSettings = AppSettingsReader.Read();
        var apiService = new VkVideoLiveApiService();
        var chatService = new VkVideoLiveChatServiceOld(apiService);

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
