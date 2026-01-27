using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using UkiChat.Model.SevenTv;

namespace UkiChat.Services;

/// <summary>
/// Сервис для работы с 7TV API
/// </summary>
public class SevenTvApiService : ISevenTvApiService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://7tv.io/v3/"; // Trailing slash is important!

    public SevenTvApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Добавляем заголовки, похожие на браузерные
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
    }

    public async Task<List<SevenTvEmote>> GetGlobalEmotesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("emote-sets/global");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var emotes = new List<SevenTvEmote>();

            if (doc.RootElement.TryGetProperty("emotes", out var emotesArray))
            {
                foreach (var emoteElement in emotesArray.EnumerateArray())
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
            Console.WriteLine($"Error fetching 7TV global emotes: {ex.Message}");
            return new List<SevenTvEmote>();
        }
    }

    public async Task<List<SevenTvEmote>> GetChannelEmotesAsync(string twitchBroadcasterId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"users/twitch/{twitchBroadcasterId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var emotes = new List<SevenTvEmote>();

            // 7TV API возвращает emote_set -> emotes
            if (doc.RootElement.TryGetProperty("emote_set", out var emoteSet) &&
                emoteSet.TryGetProperty("emotes", out var emotesArray))
            {
                foreach (var emoteElement in emotesArray.EnumerateArray())
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
            Console.WriteLine($"Error fetching 7TV channel emotes for {twitchBroadcasterId}: {ex.Message}");
            return new List<SevenTvEmote>();
        }
    }

    private static SevenTvEmote? ParseEmote(JsonElement emoteElement)
    {
        try
        {
            if (!emoteElement.TryGetProperty("id", out var idProp) ||
                !emoteElement.TryGetProperty("name", out var nameProp))
            {
                return null;
            }

            var id = idProp.GetString();
            var name = nameProp.GetString();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                return null;

            // URL формат: https://cdn.7tv.app/emote/{id}/4x.webp
            var url = $"https://cdn.7tv.app/emote/{id}/4x.webp";

            return new SevenTvEmote(id, name, url);
        }
        catch
        {
            return null;
        }
    }
}
