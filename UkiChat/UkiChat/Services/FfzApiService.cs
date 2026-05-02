using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UkiChat.Model.Ffz;

namespace UkiChat.Services;

public class FfzApiService : IFfzApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.frankerfacez.com/v1/";

    public FfzApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
    }

    public async Task<List<FfzEmote>> GetGlobalEmotesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("set/global");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var emotes = new List<FfzEmote>();

            // default_sets содержит ID наборов, которые нужно загрузить
            if (!doc.RootElement.TryGetProperty("default_sets", out var defaultSets) ||
                !doc.RootElement.TryGetProperty("sets", out var sets))
                return emotes;

            foreach (var setId in defaultSets.EnumerateArray())
            {
                var setKey = setId.GetRawText();
                if (!sets.TryGetProperty(setKey, out var set)) continue;
                if (!set.TryGetProperty("emoticons", out var emoticons)) continue;

                foreach (var emoteElement in emoticons.EnumerateArray())
                {
                    var emote = ParseEmote(emoteElement);
                    if (emote != null)
                        emotes.Add(emote);
                }
            }

            return emotes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching FFZ global emotes: {ex.Message}");
            return new List<FfzEmote>();
        }
    }

    public async Task<List<FfzEmote>> GetChannelEmotesAsync(string twitchBroadcasterId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"room/id/{twitchBroadcasterId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var emotes = new List<FfzEmote>();

            if (!doc.RootElement.TryGetProperty("room", out var room) ||
                !doc.RootElement.TryGetProperty("sets", out var sets))
                return emotes;

            if (!room.TryGetProperty("set", out var setIdProp))
                return emotes;

            var setKey = setIdProp.GetRawText();
            if (!sets.TryGetProperty(setKey, out var set)) return emotes;
            if (!set.TryGetProperty("emoticons", out var emoticons)) return emotes;

            foreach (var emoteElement in emoticons.EnumerateArray())
            {
                var emote = ParseEmote(emoteElement);
                if (emote != null)
                    emotes.Add(emote);
            }

            return emotes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching FFZ channel emotes for {twitchBroadcasterId}: {ex.Message}");
            return new List<FfzEmote>();
        }
    }

    private static FfzEmote? ParseEmote(JsonElement emoteElement)
    {
        try
        {
            if (!emoteElement.TryGetProperty("id", out var idProp) ||
                !emoteElement.TryGetProperty("name", out var nameProp))
                return null;

            var id = idProp.GetInt32().ToString();
            var name = nameProp.GetString();

            if (string.IsNullOrEmpty(name))
                return null;

            var url = $"https://cdn.frankerfacez.com/emoticon/{id}/4";
            return new FfzEmote(id, name, url);
        }
        catch
        {
            return null;
        }
    }
}
