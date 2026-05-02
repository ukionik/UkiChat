using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UkiChat.Model.Bttv;

namespace UkiChat.Services;

public class BttvApiService : IBttvApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.betterttv.net/3/";

    public BttvApiService()
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

    public async Task<List<BttvEmote>> GetGlobalEmotesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("cached/emotes/global");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var emotes = new List<BttvEmote>();

            // Глобальный ответ — массив эмоутов
            foreach (var emoteElement in doc.RootElement.EnumerateArray())
            {
                var emote = ParseEmote(emoteElement);
                if (emote != null)
                    emotes.Add(emote);
            }

            return emotes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching BTTV global emotes: {ex.Message}");
            return new List<BttvEmote>();
        }
    }

    public async Task<List<BttvEmote>> GetChannelEmotesAsync(string twitchBroadcasterId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"cached/users/twitch/{twitchBroadcasterId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var emotes = new List<BttvEmote>();

            // Объединяем channelEmotes и sharedEmotes
            if (doc.RootElement.TryGetProperty("channelEmotes", out var channelEmotes))
                foreach (var emoteElement in channelEmotes.EnumerateArray())
                {
                    var emote = ParseEmote(emoteElement);
                    if (emote != null)
                        emotes.Add(emote);
                }

            if (doc.RootElement.TryGetProperty("sharedEmotes", out var sharedEmotes))
                foreach (var emoteElement in sharedEmotes.EnumerateArray())
                {
                    var emote = ParseEmote(emoteElement);
                    if (emote != null)
                        emotes.Add(emote);
                }

            return emotes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching BTTV channel emotes for {twitchBroadcasterId}: {ex.Message}");
            return new List<BttvEmote>();
        }
    }

    private static BttvEmote? ParseEmote(JsonElement emoteElement)
    {
        try
        {
            if (!emoteElement.TryGetProperty("id", out var idProp) ||
                !emoteElement.TryGetProperty("code", out var codeProp))
                return null;

            var id = idProp.GetString();
            var name = codeProp.GetString();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                return null;

            var url = $"https://cdn.betterttv.net/emote/{id}/3x";
            return new BttvEmote(id, name, url);
        }
        catch
        {
            return null;
        }
    }
}
