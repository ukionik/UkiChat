using System.IO;
using Tomlyn;

namespace UkiChat.Tests.AppSettingsData;

public static class AppSettingsReader
{
    public static AppSettings Read()
    {
        var tomlContent = File.ReadAllText("app-settings.local.toml");
        return Toml.ToModel<AppSettings>(tomlContent);
    }
}