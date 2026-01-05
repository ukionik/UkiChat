using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using UkiChat.Configuration;
using UkiChat.Hubs;
using UkiChat.Model.Chat;

namespace UkiChat.Services;

public class StreamService : IStreamService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly TwitchClient _twitchClient;

    public StreamService(IDatabaseContext databaseContext
    , ISignalRService signalRService)
    {
        _databaseContext = databaseContext;
        _twitchClient = new TwitchClient();
        _twitchClient.OnMessageReceived += async (sender, e) =>
        {
            await signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessage(e.ChatMessage));
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