using System.Collections.Generic;

namespace UkiChat.Repositories.Memory;

public class TwitchChannelPointsRewardsRepository : ITwitchChannelPointsRewardsRepository
{
    private readonly Dictionary<string, Dictionary<string, string>> _rewardsByBroadcaster = new();

    public void SetRewards(string broadcasterId, Dictionary<string, string> rewardIdToTitle)
    {
        _rewardsByBroadcaster[broadcasterId] = rewardIdToTitle;
    }

    public string? GetRewardTitle(string broadcasterId, string rewardId)
    {
        if (_rewardsByBroadcaster.TryGetValue(broadcasterId, out var rewards) &&
            rewards.TryGetValue(rewardId, out var title))
            return title;
        return null;
    }
}
