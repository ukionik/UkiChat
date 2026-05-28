using System.Collections.Generic;
using UkiChat.Model.Twitch;

namespace UkiChat.Repositories.Memory;

public interface ITwitchChannelPointsRewardsRepository
{
    void SetRewards(string broadcasterId, Dictionary<string, TwitchChannelPointReward> rewards);
    TwitchChannelPointReward? GetReward(string broadcasterId, string rewardId);
}
