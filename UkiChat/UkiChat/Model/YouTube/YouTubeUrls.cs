using System;

namespace UkiChat.Model.YouTube;

public static class YouTubeUrls
{
    public const string BaseUrl = "https://www.youtube.com";

    public const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    /// <summary>
    ///     URL страницы /live для канала: "@handle", "handle", "UCxxxx" (channelId) или полный URL.
    ///     Watch-страница, на которую ведёт /live, содержит и videoId, и счётчик зрителей.
    /// </summary>
    public static string BuildLiveUrl(string channel)
    {
        channel = channel.Trim();
        if (channel.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return channel.TrimEnd('/') + "/live";
        if (channel.StartsWith("UC", StringComparison.Ordinal) && channel.Length == 24)
            return $"{BaseUrl}/channel/{channel}/live";
        var handle = channel.StartsWith('@') ? channel : "@" + channel;
        return $"{BaseUrl}/{handle}/live";
    }
}
