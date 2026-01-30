using System.Text.Json.Serialization;

namespace UkiChat.Model.VkVideoLive;

/// <summary>
/// Ответ от VK Video Live API при получении информации о канале
/// </summary>
public record VkVideoLiveChannelInfoResponse
{
    /// <summary>
    /// Данные о канале
    /// </summary>
    [JsonPropertyName("data")]
    public VkVideoLiveChannelData Data { get; init; } = new();
}

/// <summary>
/// Данные о канале и связанных объектах
/// </summary>
public record VkVideoLiveChannelData
{
    /// <summary>
    /// Информация о канале
    /// </summary>
    [JsonPropertyName("channel")]
    public VkVideoLiveChannel Channel { get; init; } = new();

    /// <summary>
    /// Информация о владельце канала
    /// </summary>
    [JsonPropertyName("owner")]
    public VkVideoLiveOwner Owner { get; init; } = new();

    /// <summary>
    /// Информация о текущем стриме
    /// </summary>
    [JsonPropertyName("stream")]
    public VkVideoLiveStream? Stream { get; init; }
}

/// <summary>
/// Информация о канале VK Video Live
/// </summary>
public record VkVideoLiveChannel
{
    /// <summary>
    /// ID канала
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// URL канала
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Ник канала
    /// </summary>
    [JsonPropertyName("nick")]
    public string Nick { get; init; } = string.Empty;

    /// <summary>
    /// Описание канала
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Статус канала (online/offline)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// URL аватара канала
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// URL обложки канала
    /// </summary>
    [JsonPropertyName("cover_url")]
    public string? CoverUrl { get; init; }

    /// <summary>
    /// Цвет ника
    /// </summary>
    [JsonPropertyName("nick_color")]
    public int NickColor { get; init; }

    /// <summary>
    /// Счетчики канала
    /// </summary>
    [JsonPropertyName("counters")]
    public VkVideoLiveChannelCounters? Counters { get; init; }

    /// <summary>
    /// WebSocket каналы для подписки на события
    /// </summary>
    [JsonPropertyName("web_socket_channels")]
    public VkVideoLiveWebSocketChannels? WebSocketChannels { get; init; }
}

/// <summary>
/// Счетчики канала
/// </summary>
public record VkVideoLiveChannelCounters
{
    /// <summary>
    /// Количество подписчиков
    /// </summary>
    [JsonPropertyName("subscribers")]
    public int Subscribers { get; init; }
}

/// <summary>
/// WebSocket каналы для подписки на события канала
/// </summary>
public record VkVideoLiveWebSocketChannels
{
    /// <summary>
    /// Канал чата
    /// </summary>
    [JsonPropertyName("chat")]
    public string Chat { get; init; } = string.Empty;

    /// <summary>
    /// Приватный канал чата
    /// </summary>
    [JsonPropertyName("private_chat")]
    public string PrivateChat { get; init; } = string.Empty;

    /// <summary>
    /// Канал информации
    /// </summary>
    [JsonPropertyName("info")]
    public string Info { get; init; } = string.Empty;

    /// <summary>
    /// Приватный канал информации
    /// </summary>
    [JsonPropertyName("private_info")]
    public string PrivateInfo { get; init; } = string.Empty;

    /// <summary>
    /// Канал баллов канала
    /// </summary>
    [JsonPropertyName("channel_points")]
    public string ChannelPoints { get; init; } = string.Empty;

    /// <summary>
    /// Приватный канал баллов канала
    /// </summary>
    [JsonPropertyName("private_channel_points")]
    public string PrivateChannelPoints { get; init; } = string.Empty;

    /// <summary>
    /// Ограниченный канал чата (для конкретного стрима)
    /// </summary>
    [JsonPropertyName("limited_chat")]
    public string LimitedChat { get; init; } = string.Empty;

    /// <summary>
    /// Приватный ограниченный канал чата
    /// </summary>
    [JsonPropertyName("limited_private_chat")]
    public string LimitedPrivateChat { get; init; } = string.Empty;
}

/// <summary>
/// Информация о владельце канала
/// </summary>
public record VkVideoLiveOwner
{
    /// <summary>
    /// ID владельца
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>
    /// Ник владельца
    /// </summary>
    [JsonPropertyName("nick")]
    public string Nick { get; init; } = string.Empty;

    /// <summary>
    /// URL аватара владельца
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// Цвет ника
    /// </summary>
    [JsonPropertyName("nick_color")]
    public int NickColor { get; init; }

    /// <summary>
    /// Является ли верифицированным стримером
    /// </summary>
    [JsonPropertyName("is_verified_streamer")]
    public bool IsVerifiedStreamer { get; init; }
}

/// <summary>
/// Информация о стриме
/// </summary>
public record VkVideoLiveStream
{
    /// <summary>
    /// ID стрима
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Название стрима
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Статус стрима (online/offline)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Время начала стрима (Unix timestamp)
    /// </summary>
    [JsonPropertyName("started_at")]
    public long StartedAt { get; init; }

    /// <summary>
    /// Время окончания стрима (Unix timestamp)
    /// </summary>
    [JsonPropertyName("ended_at")]
    public long EndedAt { get; init; }

    /// <summary>
    /// URL превью стрима
    /// </summary>
    [JsonPropertyName("preview_url")]
    public string? PreviewUrl { get; init; }
}
