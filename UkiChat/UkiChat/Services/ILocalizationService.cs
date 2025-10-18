using System;

namespace UkiChat.Services;

public interface ILocalizationService
{    
    string GetString(string key);
    void SetCulture(string culture);
    event EventHandler LanguageChanged;
}