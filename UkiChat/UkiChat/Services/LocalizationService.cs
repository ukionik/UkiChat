using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UkiChat.Services;

public class LocalizationService : ILocalizationService
{
    private Dictionary<string, JsonElement>? _currentStrings = new();

    public void SetCulture(string culture)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Localization", $"{culture}.json");
        var json = File.ReadAllText(filePath);
        _currentStrings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? LanguageChanged;

    public string GetString(string key)
    {
        // Поддержка вложенных ключей через точку, например "button.save"
        var parts = key.Split('.');
        if (!_currentStrings!.TryGetValue(parts[0], out var current))
            return key;

        for (var i = 1; i < parts.Length; i++)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(parts[i], out var next))
            {
                current = next;
            }
            else
            {
                return key;
            }
        }

        return current.GetString() ?? key;
    }
}