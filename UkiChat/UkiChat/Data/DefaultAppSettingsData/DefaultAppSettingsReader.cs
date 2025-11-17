using System.IO;
using System.Reflection;
using Tomlyn;

namespace UkiChat.Data.DefaultAppSettingsData;

public static class DefaultAppSettingsReader
{
    public static DefaultAppSettings Read()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("UkiChat.app-settings.local.toml");
        using var reader = new StreamReader(stream);
        var tomlContent = reader.ReadToEnd();
        return Toml.ToModel<DefaultAppSettings>(tomlContent);
    }    
}