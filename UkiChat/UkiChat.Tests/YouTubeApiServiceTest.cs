using System.Threading.Tasks;
using UkiChat.Services;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class YouTubeApiServiceTest(ITestOutputHelper testOutputHelper)
{
    private const string TestChannel = "@aljazeeraenglish";

    [Fact]
    public async Task GetViewerCountAsync_ReturnsCount_ForLiveChannel()
    {
        var api = new YouTubeApiService();
        var count = await api.GetViewerCountAsync(TestChannel);

        testOutputHelper.WriteLine($"YouTube viewer count for {TestChannel}: {count?.ToString() ?? "<offline>"}");
        Assert.True(count is > 0, "Ожидали положительное число зрителей у активной трансляции");
    }
}
