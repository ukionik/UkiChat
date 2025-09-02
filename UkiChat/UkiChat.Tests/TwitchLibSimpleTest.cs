using System;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class TwitchLibSimpleTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task ConnectionTest()
    {
        var credentials = new ConnectionCredentials("", "");
        var client = new TwitchClient();
        client.Initialize(credentials, "");

        client.OnMessageReceived += (sender, e) =>
        {
            testOutputHelper.WriteLine($"[{e.ChatMessage.DisplayName}]: {e.ChatMessage.Message}");
            return null!;
        };

        await client.ConnectAsync();
        await Task.Delay(TimeSpan.FromSeconds(100));
    }
}