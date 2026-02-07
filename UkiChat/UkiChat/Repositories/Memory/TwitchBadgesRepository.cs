using System.Collections.Generic;
using System.Linq;
using TwitchLib.Api.Helix.Models.Chat.Badges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;

namespace UkiChat.Repositories.Memory;

/// <summary>
/// Репозиторий для хранения бейджей Twitch чата в памяти
/// </summary>
public class TwitchBadgesRepository : ITwitchBadgesRepository
{
    // Dictionary<SetId, BadgeVersion[]>
    private Dictionary<string, BadgeVersion[]> _globalBadges = new();

    // Dictionary<BroadcasterId, Dictionary<SetId, BadgeVersion[]>>
    private readonly Dictionary<string, Dictionary<string, BadgeVersion[]>> _channelBadges = new();

    public Dictionary<string, BadgeVersion[]> GetGlobalBadges()
    {
        return _globalBadges;
    }

    public Dictionary<string, BadgeVersion[]> GetChannelBadges(string broadcasterId)
    {
        return _channelBadges.TryGetValue(broadcasterId, out var badges)
            ? badges
            : new Dictionary<string, BadgeVersion[]>();
    }

    public void SetGlobalBadges(GetGlobalChatBadgesResponse badges)
    {
        _globalBadges = badges.EmoteSet
            .ToDictionary(
                emoteSet => emoteSet.SetId,
                emoteSet => emoteSet.Versions
            );
    }

    public void SetChannelBadges(string broadcasterId, GetChannelChatBadgesResponse badges)
    {
        _channelBadges[broadcasterId] = badges.EmoteSet
            .ToDictionary(
                emoteSet => emoteSet.SetId,
                emoteSet => emoteSet.Versions
            );
    }

    public List<string> GetBadgeUrls(ICollection<KeyValuePair<string, string>> badges, string broadcasterId)
    {
        var badgeUrls = new List<string>();

        if (badges.Count == 0)
            return badgeUrls;

        var channelBadges = !string.IsNullOrEmpty(broadcasterId) && _channelBadges.TryGetValue(broadcasterId, out var channelBadge)
            ? channelBadge
            : null;

        foreach (var (setId, versionId) in badges)
        {
            BadgeVersion[]? badgeVersions = null;

            // Сначала ищем в бейджах канала, затем в глобальных
            if (channelBadges != null && channelBadges.TryGetValue(setId, out var channelBadgeVersions))
            {
                badgeVersions = channelBadgeVersions;
            }
            else if (_globalBadges.TryGetValue(setId, out var globalBadgeVersions))
            {
                badgeVersions = globalBadgeVersions;
            }

            // Ищем конкретную версию по badge.Value
            if (badgeVersions is { Length: > 0 })
            {
                var badgeVersion = badgeVersions.FirstOrDefault(v => v.Id == versionId);
                if (badgeVersion != null)
                {
                    badgeUrls.Add(badgeVersion.ImageUrl4x);
                }
            }
        }

        return badgeUrls;
    }

    public void Clear()
    {
        _globalBadges.Clear();
        _channelBadges.Clear();
    }
}
