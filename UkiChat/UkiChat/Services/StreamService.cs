using System;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using UkiChat.Configuration;
using UkiChat.Model.Chat;

namespace UkiChat.Services;

public class StreamService : IStreamService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly ISignalRService _signalRService;
    private readonly ILocalizationService _localizationService;
    private readonly TwitchClient _twitchClient = new();
    private string _channelName = "";

    public StreamService(IDatabaseContext databaseContext
        , ISignalRService signalRService
        , ILocalizationService localizationService)
    {
        _databaseContext = databaseContext;
        _signalRService = signalRService;
        _localizationService = localizationService;
        _twitchClient.OnMessageReceived += async (sender, e) =>
        {
            await signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessage(e.ChatMessage));
        };

        _twitchClient.OnError += async (sender, e) =>
        {
            Console.WriteLine(e.Exception.ToString());
            /*await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.error"), _channelName));*/
        };

        _twitchClient.OnJoinedChannel += async (sender, e) =>
        {
            Console.WriteLine("JoinedChannel");
            await SendChatMessageNotification(string.Format(_localizationService.GetString("twitch.connectedToChannel"),
                e.Channel));
        };

        _twitchClient.OnLeftChannel += async (sender, e) =>
        {
            Console.WriteLine("Disconnected");
            await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.disconnectedFromChannel"), e.Channel));
        };

        _twitchClient.OnDisconnected += async (sender, e) =>
        {
            Console.WriteLine("Disconnected");
            /*await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.disconnectedFromChannel")));*/
        };

        _twitchClient.OnConnectionError += async (sender, e) =>
        {
            Console.WriteLine("ConnectionError");
            /*await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.connectingToChannelError"), _channelName));*/
        };
    }

    public async Task ConnectToTwitchAsync()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var oldChannel = _channelName;
        var newChannel = twitchSettings.Channel;
        
        if (!_twitchClient.IsConnected)
        {
            var credentials = new ConnectionCredentials(twitchSettings.ChatbotUsername, twitchSettings.ChatbotAccessToken);
            _twitchClient.Initialize(credentials);
            await _twitchClient.ConnectAsync();            
        }

        if (oldChannel == newChannel)
        {
            return;
        }
        
        if (_twitchClient.JoinedChannels.Any(x => x.Channel == oldChannel))
            await _twitchClient.LeaveChannelAsync(oldChannel);
        
        if (newChannel.Length == 0)
        {
            return;
        }

        _channelName = newChannel;
        await SendChatMessageNotification(string.Format(_localizationService.GetString("twitch.connectingToChannel"),
            _channelName));
        
        await _twitchClient.JoinChannelAsync(newChannel, true);
    }

    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
}