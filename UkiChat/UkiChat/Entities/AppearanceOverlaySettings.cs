namespace UkiChat.Entities;

public class AppearanceOverlaySettings
{
    public int Scale { get; set; } = 100;
    public string Theme { get; set; } = "default";
    public int MessageHideDelay { get; set; } = 0;
    public bool HideClippedMessages { get; set; } = false;
}
