using System;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using UkiChat.Tests.AppSettingsData;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class TwitchChatTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task ConnectionTest()
    {
        var appSettings = AppSettingsReader.Read();
        var credentials = new ConnectionCredentials(appSettings.Twitch.Chat.Username, appSettings.Twitch.Chat.AccessToken);
        var client = new TwitchClient();
        client.Initialize(credentials, appSettings.Twitch.Chat.Channel);

        client.OnMessageReceived += (sender, e) =>
        {
            testOutputHelper.WriteLine($"[{e.ChatMessage.DisplayName}]: {e.ChatMessage.Message}");
            return null!;
        };

        client.OnError += (sender, e) =>
        {
            testOutputHelper.WriteLine(e.Exception.ToString());
            return null!;
        };
        
        client.OnConnected += (sender, e) =>
        {
            testOutputHelper.WriteLine("Connected");
            return null!;
        };
        
        client.OnDisconnected += (sender, e) =>
        {
            testOutputHelper.WriteLine("Disconnected");
            return null!;
        };
        
        client.OnConnectionError += (sender, e) =>
        {
            testOutputHelper.WriteLine("ConnectionError");
            return null!;
        };
        
        client.OnJoinedChannel += (sender, e) =>
        {
            testOutputHelper.WriteLine("JoinedChannel");
            return null!;
        };


        await client.ConnectAsync();
        await Task.Delay(TimeSpan.FromMinutes(5));
    }
}