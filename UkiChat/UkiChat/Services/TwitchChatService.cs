using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using UkiChat.Configuration;
using UkiChat.Entities;
using UkiChat.Model.Chat;
using UkiChat.Model.SevenTv;
using UkiChat.Model.Twitch;
using UkiChat.Repositories.Memory;

namespace UkiChat.Services;

public class TwitchChatService : ITwitchChatService
{
    private readonly IDatabaseContext _databaseContext;
    private readonly ISevenTvApiService _sevenTvApiService;
    private readonly ISevenTvEmotesRepository _sevenTvEmotesRepository;
    private readonly ISignalRService _signalRService;
    private readonly ITwitchApiService _twitchApiService;
    private readonly ITwitchBadgesRepository _twitchBadgesRepository;
    private readonly TwitchClient _twitchClient = new();
    private string _broadcasterId = "";
    private string _channelName = "";

    public TwitchChatService(IDatabaseContext databaseContext
        , ISignalRService signalRService
        , ILocalizationService localizationService
        , ISevenTvApiService sevenTvApiService
        , ITwitchBadgesRepository twitchBadgesRepository
        , ISevenTvEmotesRepository sevenTvEmotesRepository
        , ITwitchApiService twitchApiService
    )
    {
        _databaseContext = databaseContext;
        _signalRService = signalRService;
        _sevenTvApiService = sevenTvApiService;
        _twitchBadgesRepository = twitchBadgesRepository;
        _sevenTvEmotesRepository = sevenTvEmotesRepository;
        _twitchApiService = twitchApiService;

        _twitchClient.OnMessageReceived += async (_, e) =>
        {
            var badgeUrls = ResolveBadgeUrls(e.ChatMessage);
            var sevenTvEmotes = GetSevenTvEmotes();
            Console.WriteLine(
                $"[Twitch] Message received from: {e.ChatMessage.DisplayName}. Message: {e.ChatMessage.Message}");
            await signalRService.SendChatMessageAsync(
                UkiChatMessage.FromTwitchMessage(e.ChatMessage, badgeUrls, sevenTvEmotes));
        };

        _twitchClient.OnError += (_, e) =>
        {
            Console.WriteLine(e.Exception.ToString());
            return Task.CompletedTask;
            /*await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.error"), _channelName));*/
        };

        _twitchClient.OnJoinedChannel += async (_, e) =>
        {
            Console.WriteLine("JoinedChannel");
            await SendChatMessageNotification(string.Format(localizationService.GetString("twitch.connectedToChannel"),
                e.Channel));
        };

        _twitchClient.OnLeftChannel += async (_, e) =>
        {
            Console.WriteLine("Disconnected");
            await SendChatMessageNotification(
                string.Format(localizationService.GetString("twitch.disconnectedFromChannel"), e.Channel));
        };

        _twitchClient.OnDisconnected += (_, _) =>
        {
            Console.WriteLine("Disconnected");
            return Task.CompletedTask;
            /*await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.disconnectedFromChannel")));*/
        };

        _twitchClient.OnConnectionError += (_, _) =>
        {
            Console.WriteLine("ConnectionError");
            return Task.CompletedTask;
            /*await SendChatMessageNotification(
                string.Format(_localizationService.GetString("twitch.connectingToChannelError"), _channelName));*/
        };
    }

    public async Task ConnectAsync(TwitchConnectionParams connectionParams)
    {
        if (!_twitchClient.IsConnected)
        {
            var credentials =
                new ConnectionCredentials(connectionParams.ChatbotUsername, connectionParams.ChatbotAccessToken);
            _twitchClient.Initialize(credentials);
            await _twitchClient.ConnectAsync();
        }

        if (_twitchClient.JoinedChannels.Any(x => x.Channel == connectionParams.OldChannel))
            await _twitchClient.LeaveChannelAsync(connectionParams.OldChannel);
        
        if (connectionParams.NewChannel == "")
            return;

        await _twitchClient.JoinChannelAsync(connectionParams.NewChannel, true);
    }

    public async Task ChangeChannelAsync(string newChannel)
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        var oldChannel = twitchSettings.Channel;
        if (oldChannel == newChannel)
            return;

        if (newChannel.Length == 0)
            return;

        _channelName = newChannel;
        _broadcasterId = await LoadBroadcasterIdAsync(newChannel);
        UpdateTwitchDbSettings(twitchSettings);
        await LoadChannelDataAsync();
        await ConnectAsync(TwitchConnectionParams.OfTwitchSettings(oldChannel ?? "", newChannel, twitchSettings));
    }

    public async Task LoadGlobalDataAsync()
    {
        await LoadTwitchGlobalBadgesAsync();
        await LoadSevenTvGlobalEmotesAsync();
    }

    public async Task LoadChannelDataAsync()
    {
        var twitchSettings = _databaseContext.TwitchSettingsRepository.GetActiveSettings();
        if (string.IsNullOrEmpty(twitchSettings.Channel))
            return;

        await LoadTwitchChannelBadgesAsync(twitchSettings);
        await LoadSevenTvChannelEmotesAsync(twitchSettings);
    }

    private async Task LoadTwitchGlobalBadgesAsync()
    {
        try
        {
            var twitchGlobalBadges = await _twitchApiService.GetGlobalChatBadgesAsync();
            _twitchBadgesRepository.SetGlobalBadges(twitchGlobalBadges);
            Console.WriteLine($"Loaded {twitchGlobalBadges.EmoteSet.Length} global badge sets");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading twitch global badges: {e.Message}");
        }
    }

    private async Task LoadSevenTvGlobalEmotesAsync()
    {
        try
        {
            var globalEmotes = await _sevenTvApiService.GetGlobalEmotesAsync();
            _sevenTvEmotesRepository.SetGlobalEmotes(globalEmotes);
            Console.WriteLine($"Loaded {globalEmotes.Count} global 7TV emotes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading Global 7TV emotes: {ex.Message}");
        }
    }

    private async Task LoadTwitchChannelBadgesAsync(TwitchSettings twitchSettings)
    {
        try
        {
            if (string.IsNullOrEmpty(twitchSettings.ApiBroadcasterId))
                return;

            var channelBadges = await _twitchApiService.GetChannelChatBadgesAsync(twitchSettings.ApiBroadcasterId);
            _twitchBadgesRepository.SetChannelBadges(twitchSettings.ApiBroadcasterId, channelBadges);
            Console.WriteLine($"Loaded {channelBadges.EmoteSet.Length} channel badge sets for {twitchSettings.Channel}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading twitch chat badges: {e.Message}");
        }
    }

    private async Task LoadSevenTvChannelEmotesAsync(TwitchSettings twitchSettings)
    {
        try
        {
            if (string.IsNullOrEmpty(twitchSettings.ApiBroadcasterId))
                return;

            var channelEmotes = await _sevenTvApiService.GetChannelEmotesAsync(twitchSettings.ApiBroadcasterId);
            _sevenTvEmotesRepository.SetChannelEmotes(twitchSettings.ApiBroadcasterId, channelEmotes);
            Console.WriteLine($"Loaded {channelEmotes.Count} channel 7TV emotes for {twitchSettings.Channel}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading channel=${twitchSettings.Channel} 7TV emotes: {ex.Message}");
        }
    }

    private void UpdateTwitchDbSettings(TwitchSettings twitchSettings)
    {
        twitchSettings.Channel = _channelName;
        twitchSettings.ApiBroadcasterId = _broadcasterId;
        _databaseContext.TwitchSettingsRepository.Save(twitchSettings);
    }

    private async Task<string> LoadBroadcasterIdAsync(string newChannel)
    {
        try
        {
            return await _twitchApiService.GetBroadcasterIdAsync(newChannel);
        }
        catch (Exception)
        {
            Console.WriteLine($"Can't load Twitch API Broadcaster Id for Channel={newChannel}");
            return "";
        }
    }

    private List<string> ResolveBadgeUrls(ChatMessage chatMessage)
    {
        return _twitchBadgesRepository.GetBadgeUrls(chatMessage.Badges, _broadcasterId);
    }

    private Dictionary<string, SevenTvEmote> GetSevenTvEmotes()
    {
        var emotes = new Dictionary<string, SevenTvEmote>();

        foreach (var (name, emote) in _sevenTvEmotesRepository.GetGlobalEmotes()) emotes[name] = emote;

        if (string.IsNullOrEmpty(_broadcasterId))
            return emotes;

        foreach (var (name, emote) in _sevenTvEmotesRepository.GetChannelEmotes(_broadcasterId)) emotes[name] = emote;

        return emotes;
    }

    private async Task SendChatMessageNotification(string message)
    {
        await _signalRService.SendChatMessageAsync(UkiChatMessage.FromTwitchMessageNotification(message));
    }
}