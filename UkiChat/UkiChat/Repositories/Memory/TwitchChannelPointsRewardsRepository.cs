using System.Collections.Generic;
using UkiChat.Model.Twitch;

namespace UkiChat.Repositories.Memory;

public class TwitchChannelPointsRewardsRepository : ITwitchChannelPointsRewardsRepository
{
    private readonly Dictionary<string, Dictionary<string, TwitchChannelPointReward>> _rewardsByBroadcaster = new();

    public void SetRewards(string broadcasterId, Dictionary<string, TwitchChannelPointReward> rewards)
    {
        _rewardsByBroadcaster[broadcasterId] = rewards;
    }

    public TwitchChannelPointReward? GetReward(string broadcasterId, string rewardId)
    {
        if (_rewardsByBroadcaster.TryGetValue(broadcasterId, out var rewards) &&
            rewards.TryGetValue(rewardId, out var reward))
            return reward;
        return null;
    }
}
