using System.Collections.Generic;

namespace UkiChat.Repositories.Memory;

public interface ITwitchChannelPointsRewardsRepository
{
    void SetRewards(string broadcasterId, Dictionary<string, string> rewardIdToTitle);
    string? GetRewardTitle(string broadcasterId, string rewardId);
}
