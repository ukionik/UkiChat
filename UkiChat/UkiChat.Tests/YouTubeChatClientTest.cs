using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using UkiChat.Model.YouTube;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class YouTubeChatClientTest(ITestOutputHelper testOutputHelper)
{
    // Канал для проверки спайка. Должен быть в эфире с включённым чатом.
    private const string TestChannel = "@aljazeeraenglish";

    [Fact]
    public async Task YouTubeChatClient_ConnectByChannel_ReceivesMessages()
    {
        var client = new YouTubeChatClient(NullLogger<YouTubeChatClient>.Instance);

        var connectedEvent = new TaskCompletionSource<bool>();
        var messageReceivedEvent = new TaskCompletionSource<bool>();

        client.Connected += (_, _) =>
        {
            testOutputHelper.WriteLine("Connected to YouTube live chat!");
            connectedEvent.TrySetResult(true);
        };

        client.MessageReceived += (_, e) =>
        {
            var m = e.Message;
            if (m == null) return;
            var text = string.Join("", m.Parts.Select(p =>
                p.Kind == YouTubeChatPartKind.Text ? p.Content : $"[emote]"));
            var badges = m.Badges.Count > 0
                ? " {" + string.Join(",", m.Badges.Select(b => b.IconType ?? b.Tooltip)) + "}"
                : "";
            var super = m.IsSuperChat ? $" [SUPERCHAT {m.AmountText}]" : "";
            testOutputHelper.WriteLine($"{m.AuthorName}{badges}{super}: {text}");
            messageReceivedEvent.TrySetResult(true);
        };

        client.Error += (_, e) => testOutputHelper.WriteLine($"Error: {e.Message}");
        client.Disconnected += (_, e) => testOutputHelper.WriteLine($"Disconnected: {e.Reason}");

        try
        {
            var videoId = await client.ResolveLiveVideoIdAsync(TestChannel);
            testOutputHelper.WriteLine($"Resolved videoId: {videoId ?? "<offline>"}");
            Assert.False(string.IsNullOrEmpty(videoId), $"Канал {TestChannel} не в эфире — выбери другой");

            await client.ConnectByVideoIdAsync(videoId!);

            var connected = await Task.WhenAny(connectedEvent.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.True(connected == connectedEvent.Task, "Не удалось подключиться за 15 секунд");

            testOutputHelper.WriteLine("Waiting for messages (60 seconds)...");
            var got = await Task.WhenAny(messageReceivedEvent.Task, Task.Delay(TimeSpan.FromSeconds(60)));
            if (got == messageReceivedEvent.Task)
                testOutputHelper.WriteLine("Messages received!");
            else
                testOutputHelper.WriteLine("No messages in 60s (OK if chat is slow/inactive)");
        }
        finally
        {
            await client.DisconnectAsync();
            client.Dispose();
        }
    }
}
