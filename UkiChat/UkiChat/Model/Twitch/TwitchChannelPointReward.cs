namespace UkiChat.Model.Twitch;

/// <summary>
/// Кастомная награда за баллы канала (название + стоимость).
/// </summary>
public record TwitchChannelPointReward(string Title, int Cost);
