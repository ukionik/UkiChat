namespace UkiChat.Entities;

public class TwitchGlobalSettings
{
    public int Id { get; private set; } = 1;
    public string TwitchChatBotUsername { get; set; }
    public string TwitchChatBotAccessToken { get; set; }
}