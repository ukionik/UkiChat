using System.Collections.Generic;
using System.Linq;
using TwitchLib.Api.Helix.Models.Chat.Badges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetChannelChatBadges;
using TwitchLib.Api.Helix.Models.Chat.Badges.GetGlobalChatBadges;
using UkiChat.Entities;

namespace UkiChat.Repositories.Memory;

/// <summary>
/// Репозиторий для хранения бейджей Twitch чата в памяти
/// </summary>
public class TwitchBadgesRepository : ITwitchBadgesRepository
{
    // Dictionary<SetId, Dictionary<VersionId, ImageUrl>>
    private Dictionary<string, Dictionary<string, string>> _globalBadges = new();

    // Dictionary<BroadcasterId, Dictionary<SetId, Dictionary<VersionId, ImageUrl>>>
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _channelBadges = new();

    public void SetGlobalBadges(GetGlobalChatBadgesResponse badges)
    {
        _globalBadges = ToBadgeMap(badges.EmoteSet);
    }

    public void SetGlobalBadges(IEnumerable<TwitchBadgeEntity> badges)
    {
        _globalBadges = ToBadgeMap(badges);
    }

    public void SetChannelBadges(string broadcasterId, GetChannelChatBadgesResponse badges)
    {
        _channelBadges[broadcasterId] = ToBadgeMap(badges.EmoteSet);
    }

    public void SetChannelBadges(string broadcasterId, IEnumerable<TwitchBadgeEntity> badges)
    {
        _channelBadges[broadcasterId] = ToBadgeMap(badges);
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
            Dictionary<string, string>? badgeVersions = null;

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
            if (badgeVersions != null && badgeVersions.TryGetValue(versionId, out var imageUrl))
            {
                badgeUrls.Add(imageUrl);
            }
        }

        return badgeUrls;
    }

    public void Clear()
    {
        _globalBadges.Clear();
        _channelBadges.Clear();
    }

    private static Dictionary<string, Dictionary<string, string>> ToBadgeMap(IEnumerable<BadgeEmoteSet> emoteSets)
    {
        return emoteSets.ToDictionary(
            emoteSet => emoteSet.SetId,
            emoteSet => emoteSet.Versions.ToDictionary(v => v.Id, v => v.ImageUrl4x)
        );
    }

    private static Dictionary<string, Dictionary<string, string>> ToBadgeMap(IEnumerable<TwitchBadgeEntity> badges)
    {
        return badges
            .GroupBy(b => b.SetId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(b => b.VersionId, b => b.ImageUrl)
            );
    }
}
