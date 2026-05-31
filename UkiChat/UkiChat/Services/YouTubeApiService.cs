using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UkiChat.Model.YouTube;

namespace UkiChat.Services;

/// <summary>
/// Лёгкий клиент YouTube на InnerTube/HTML. Сейчас умеет только счётчик зрителей.
/// </summary>
public class YouTubeApiService : IYouTubeApiService
{
    // На watch-странице concurrent-зрители live-трансляции лежат в "originalViewCount":"12345"
    private static readonly Regex ViewerCountRegex =
        new("\"originalViewCount\":\"(\\d+)\"", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;

    public YouTubeApiService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(YouTubeUrls.UserAgent);
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
    }

    public async Task<int?> GetViewerCountAsync(string channel)
    {
        if (string.IsNullOrEmpty(channel))
            return null;

        var html = await _httpClient.GetStringAsync(YouTubeUrls.BuildLiveUrl(channel));
        var match = ViewerCountRegex.Match(html);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
            return count;

        // Поля нет — трансляция не идёт
        return null;
    }
}
