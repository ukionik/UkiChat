using System.Collections.Generic;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;

namespace UkiChat.Repositories.Memory;

/// <summary>
/// Репозиторий для хранения бейджей Twitch чата в памяти
/// </summary>
public class ChatBadgesRepository : IChatBadgesRepository
{
    private GetGlobalChatBadgesResponse? _globalBadges;
    private readonly Dictionary<string, GetChannelChatBadgesResponse> _channelBadges = new();

    public GetGlobalChatBadgesResponse? GetGlobalBadges()
    {
        return _globalBadges;
    }

    public GetChannelChatBadgesResponse? GetChannelBadges(string broadcasterId)
    {
        return _channelBadges.TryGetValue(broadcasterId, out var badges) ? badges : null;
    }

    public void SetGlobalBadges(GetGlobalChatBadgesResponse badges)
    {
        _globalBadges = badges;
    }

    public void SetChannelBadges(string broadcasterId, GetChannelChatBadgesResponse badges)
    {
        _channelBadges[broadcasterId] = badges;
    }

    public void Clear()
    {
        _globalBadges = null;
        _channelBadges.Clear();
    }
}
