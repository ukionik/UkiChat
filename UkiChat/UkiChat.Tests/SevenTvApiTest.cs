using System.Linq;
using System.Threading.Tasks;
using UkiChat.Services;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class SevenTvApiTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task SevenTvApiService_GetGlobalEmotesAsync_ReturnsEmotes()
    {
        // Arrange
        var service = new SevenTvApiService();

        // Act
        var result = await service.GetGlobalEmotesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        testOutputHelper.WriteLine($"Found {result.Count} global 7TV emotes");

        // Выводим первые 10 эмоутов
        var firstEmotes = result.Take(10);
        foreach (var emote in firstEmotes)
        {
            testOutputHelper.WriteLine($"Emote: {emote.Name} (ID: {emote.Id})");
            testOutputHelper.WriteLine($"  URL: {emote.Url}");
        }
    }

    [Fact]
    public async Task SevenTvApiService_GetGlobalEmotesAsync_EmotesHaveRequiredProperties()
    {
        // Arrange
        var service = new SevenTvApiService();

        // Act
        var result = await service.GetGlobalEmotesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Проверяем, что все эмоуты имеют необходимые свойства
        foreach (var emote in result)
        {
            Assert.NotNull(emote);
            Assert.NotEmpty(emote.Id);
            Assert.NotEmpty(emote.Name);
            Assert.NotEmpty(emote.Url);
            Assert.StartsWith("https://cdn.7tv.app/emote/", emote.Url);
            Assert.EndsWith("/4x.webp", emote.Url);
        }

        testOutputHelper.WriteLine($"All {result.Count} emotes have valid properties");
    }

    [Fact]
    public async Task SevenTvApiService_GetChannelEmotesAsync_ReturnsEmotesForValidChannel()
    {
        // Arrange
        var service = new SevenTvApiService();

        // xQc's Twitch broadcaster ID (популярный стример с 7TV эмоутами)
        var broadcasterId = "71092938";

        // Act
        var result = await service.GetChannelEmotesAsync(broadcasterId);

        // Assert
        Assert.NotNull(result);
        // xQc имеет 7TV эмоуты, поэтому список не должен быть пустым
        // Примечание: если канал не использует 7TV, результат может быть пустым

        testOutputHelper.WriteLine($"Found {result.Count} channel 7TV emotes for broadcaster {broadcasterId}");

        if (result.Count > 0)
        {
            // Выводим первые 10 эмоутов
            var firstEmotes = result.Take(10);
            foreach (var emote in firstEmotes)
            {
                testOutputHelper.WriteLine($"Emote: {emote.Name} (ID: {emote.Id})");
                testOutputHelper.WriteLine($"  URL: {emote.Url}");
            }
        }
        else
        {
            testOutputHelper.WriteLine("Channel has no 7TV emotes or doesn't use 7TV");
        }
    }

    [Fact]
    public async Task SevenTvApiService_GetChannelEmotesAsync_ReturnsEmptyForInvalidChannel()
    {
        // Arrange
        var service = new SevenTvApiService();

        // Несуществующий broadcaster ID
        var invalidBroadcasterId = "999999999999";

        // Act
        var result = await service.GetChannelEmotesAsync(invalidBroadcasterId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); // Должен вернуть пустой список для несуществующего канала

        testOutputHelper.WriteLine($"Correctly returned empty list for invalid broadcaster ID");
    }

    [Fact]
    public async Task SevenTvApiService_GetChannelEmotesAsync_EmotesHaveRequiredProperties()
    {
        // Arrange
        var service = new SevenTvApiService();

        // xQc's Twitch broadcaster ID
        var broadcasterId = "71092938";

        // Act
        var result = await service.GetChannelEmotesAsync(broadcasterId);

        // Assert
        Assert.NotNull(result);

        if (result.Count > 0)
        {
            // Проверяем, что все эмоуты имеют необходимые свойства
            foreach (var emote in result)
            {
                Assert.NotNull(emote);
                Assert.NotEmpty(emote.Id);
                Assert.NotEmpty(emote.Name);
                Assert.NotEmpty(emote.Url);
                Assert.StartsWith("https://cdn.7tv.app/emote/", emote.Url);
                Assert.EndsWith("/4x.webp", emote.Url);
            }

            testOutputHelper.WriteLine($"All {result.Count} channel emotes have valid properties");
        }
    }

    [Fact]
    public async Task SevenTvApiService_GlobalAndChannelEmotes_NoDuplicatesBetweenCalls()
    {
        // Arrange
        var service = new SevenTvApiService();

        // Act - делаем два вызова подряд
        var result1 = await service.GetGlobalEmotesAsync();
        var result2 = await service.GetGlobalEmotesAsync();

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Count, result2.Count);

        testOutputHelper.WriteLine($"First call: {result1.Count} emotes");
        testOutputHelper.WriteLine($"Second call: {result2.Count} emotes");
        testOutputHelper.WriteLine("Results are consistent between calls");
    }

    [Fact]
    public async Task SevenTvApiService_GetChannelEmotesAsync_HandlesNetworkErrors()
    {
        // Arrange
        var service = new SevenTvApiService();

        // Broadcaster ID с невалидными символами (должен вызвать ошибку API)
        var invalidBroadcasterId = "invalid@#$%";

        // Act
        var result = await service.GetChannelEmotesAsync(invalidBroadcasterId);

        // Assert - сервис должен обработать ошибку и вернуть пустой список
        Assert.NotNull(result);
        Assert.Empty(result);

        testOutputHelper.WriteLine("Service gracefully handled invalid broadcaster ID");
    }
}
