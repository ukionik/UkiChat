using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UkiChat.Model.VkVideoLive;

/// <summary>
/// Сообщение чата VK Video Live
/// </summary>
public record VkVideoLiveChatMessage
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public VkVideoLiveChatMessageData? Data { get; init; }
}

/// <summary>
/// Данные сообщения чата
/// </summary>
public record VkVideoLiveChatMessageData
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("author")]
    public VkVideoLiveChatAuthor? Author { get; init; }

    [JsonPropertyName("data")]
    public List<VkVideoLiveChatContent>? Content { get; init; }

    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("isPrivate")]
    public bool IsPrivate { get; init; }

    [JsonPropertyName("threadId")]
    public string? ThreadId { get; init; }

    [JsonPropertyName("parent")]
    public VkVideoLiveChatParentMessage? Parent { get; init; }
}

/// <summary>
/// Родительское сообщение (для ответов)
/// </summary>
public record VkVideoLiveChatParentMessage
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("author")]
    public VkVideoLiveChatAuthor? Author { get; init; }

    [JsonPropertyName("data")]
    public List<VkVideoLiveChatContent>? Content { get; init; }

    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; init; }

    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; init; }

    [JsonPropertyName("isPrivate")]
    public bool IsPrivate { get; init; }
}

/// <summary>
/// Автор сообщения
/// </summary>
public record VkVideoLiveChatAuthor
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("nick")]
    public string Nick { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("nickColor")]
    public int NickColor { get; init; }

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; init; }

    [JsonPropertyName("hasAvatar")]
    public bool HasAvatar { get; init; }

    [JsonPropertyName("isOwner")]
    public bool IsOwner { get; init; }

    [JsonPropertyName("isChannelModerator")]
    public bool IsChannelModerator { get; init; }

    [JsonPropertyName("isChatModerator")]
    public bool IsChatModerator { get; init; }

    [JsonPropertyName("isVerifiedStreamer")]
    public bool IsVerifiedStreamer { get; init; }

    [JsonPropertyName("badges")]
    public List<VkVideoLiveChatBadge>? Badges { get; init; }

    [JsonPropertyName("roles")]
    public List<VkVideoLiveChatRole>? Roles { get; init; }
}

/// <summary>
/// Роль пользователя на канале
/// </summary>
public record VkVideoLiveChatRole
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    [JsonPropertyName("smallUrl")]
    public string? SmallUrl { get; init; }

    [JsonPropertyName("mediumUrl")]
    public string? MediumUrl { get; init; }

    [JsonPropertyName("largeUrl")]
    public string? LargeUrl { get; init; }
}

/// <summary>
/// Бейдж пользователя
/// </summary>
public record VkVideoLiveChatBadge
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("smallUrl")]
    public string? SmallUrl { get; init; }

    [JsonPropertyName("mediumUrl")]
    public string? MediumUrl { get; init; }

    [JsonPropertyName("largeUrl")]
    public string? LargeUrl { get; init; }

    [JsonPropertyName("achievement")]
    public VkVideoLiveChatBadgeAchievement? Achievement { get; init; }
}

/// <summary>
/// Достижение бейджа
/// </summary>
public record VkVideoLiveChatBadgeAchievement
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}

/// <summary>
/// Контент сообщения (текст, эмоут, упоминание и т.д.)
/// </summary>
public record VkVideoLiveChatContent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    // Для типа "text"
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("modificator")]
    public string Modificator { get; init; } = string.Empty;

    // Для типов "smile" и "mention" - может быть строкой (GUID) или числом
    [JsonPropertyName("id")]
    public JsonElement? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    // Для типа "smile"
    [JsonPropertyName("smallUrl")]
    public string? SmallUrl { get; init; }

    [JsonPropertyName("mediumUrl")]
    public string? MediumUrl { get; init; }

    [JsonPropertyName("largeUrl")]
    public string? LargeUrl { get; init; }

    [JsonPropertyName("imageFormat")]
    public string? ImageFormat { get; init; }

    [JsonPropertyName("isAnimated")]
    public bool IsAnimated { get; init; }

    // Для типа "mention"
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("nick")]
    public string? Nick { get; init; }

    [JsonPropertyName("blogUrl")]
    public string? BlogUrl { get; init; }

    [JsonPropertyName("nickColor")]
    public JsonElement? NickColor { get; init; }
}
