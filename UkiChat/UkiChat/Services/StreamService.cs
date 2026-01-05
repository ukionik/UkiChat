using System;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using UkiChat.Configuration;
using UkiChat.Model.Chat;

namespace UkiChat.Services;

public class StreamService(IDatabaseContext databaseContext, SignalRService signalRService) : IStreamService
{
    private TwitchClient _twitchClient = new();

    public async Task ConnectToTwitchAsync()
    {
        var twitchSettings = databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var credentials = new ConnectionCredentials(twitchSettings.ChatbotUsername, twitchSettings.ChatbotAccessToken);
        if (_twitchClient.IsConnected)
        {
            await _twitchClient.DisconnectAsync()!;            
        }
        InitTwitchClient(credentials, twitchSettings.Channel);
        await signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification($"Подключение к каналу... {twitchSettings.Channel}"));
        await _twitchClient.ConnectAsync()!;
    }

    private void InitTwitchClient(ConnectionCredentials credentials, string channel)
    {
        _twitchClient = new TwitchClient();
        _twitchClient.Initialize(credentials, channel);
        _twitchClient.OnMessageReceived += async (sender, e) =>
        {
            await signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessage(e.ChatMessage));
        };

        _twitchClient.OnError += async (sender, e) =>
        {
            Console.WriteLine(e.Exception.ToString());
            await SendChatMessageNotification($"Ошибка");
        };
        
        _twitchClient.OnConnected += async (sender, e) =>
        {
            Console.WriteLine("Connected");
            await SendChatMessageNotification($"Подключился к каналу {channel}");
        };
        
        _twitchClient.OnDisconnected += async (sender, e) =>
        {
            Console.WriteLine("Disconnected");
            await SendChatMessageNotification($"Отключился от канала {channel}");
        };
        
        _twitchClient.OnConnectionError += async (sender, e) =>
        {
            Console.WriteLine("ConnectionError");
            await SendChatMessageNotification($"Ошибка подключения к каналу {channel}");
        };
        
        _twitchClient.OnJoinedChannel += async (sender, e) =>
        {
            Console.WriteLine("JoinedChannel");
            await SendChatMessageNotification($"Отключился от канала {channel}");
        };
    }

    private async Task SendChatMessageNotification(string message)
    {
        await signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
}