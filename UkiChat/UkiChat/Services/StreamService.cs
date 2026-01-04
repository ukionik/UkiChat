using System;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using UkiChat.Configuration;

namespace UkiChat.Services;

public class StreamService : IStreamService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly TwitchClient _twitchClient;

    public StreamService(IDatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
        _twitchClient = new TwitchClient();
        _twitchClient.OnMessageReceived += (sender, e) =>
        {
            Console.WriteLine($"[{e.ChatMessage.DisplayName}]: {e.ChatMessage.Message}");
            return null!;
        };

        _twitchClient.OnError += (sender, e) =>
        {
            Console.WriteLine(e.Exception.ToString());
            return null!;
        };
        
        _twitchClient.OnConnected += (sender, e) =>
        {
            Console.WriteLine("Connected");
            return null!;
        };
        
        _twitchClient.OnDisconnected += (sender, e) =>
        {
            Console.WriteLine("Disconnected");
            return null!;
        };
        
        _twitchClient.OnConnectionError += (sender, e) =>
        {
            Console.WriteLine("ConnectionError");
            return null!;
        };
        
        _twitchClient.OnJoinedChannel += (sender, e) =>
        {
            Console.WriteLine("JoinedChannel");
            return null!;
        };
    }

    public async Task ConnectToTwitchAsync(string channel)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var credentials = new ConnectionCredentials(twitchSettings.ChatbotUsername, twitchSettings.ChatbotAccessToken);
        _twitchClient.Initialize(credentials, channel);
        await _twitchClient.ConnectAsync();
    }
}