namespace UkiChat.Entities;

public class AppearanceSettings
{
    public AppearanceMainSettings Main { get; set; } = new();
    public AppearanceOverlaySettings Overlay { get; set; } = new();
}
