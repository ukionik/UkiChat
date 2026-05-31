using System.Collections.Generic;

namespace UkiChat.Model.YouTube;

/// <summary>
///     Распарсенное сообщение YouTube live-чата (InnerTube).
///     Промежуточная модель — позже маппится в общий UkiChatMessage.
/// </summary>
public class YouTubeChatMessage
{
    public string Id { get; init; } = "";
    public string AuthorName { get; init; } = "";
    public string AuthorChannelId { get; init; } = "";
    public string AuthorPhotoUrl { get; init; } = "";

    /// <summary>Текст + эмодзи в порядке следования.</summary>
    public List<YouTubeChatPart> Parts { get; init; } = [];

    /// <summary>Бейджи автора (спонсорство, модератор, владелец, верификация).</summary>
    public List<YouTubeChatBadge> Badges { get; init; } = [];

    /// <summary>true — это суперчат (liveChatPaidMessageRenderer).</summary>
    public bool IsSuperChat { get; init; }

    /// <summary>Сумма суперчата как её отдаёт YouTube, например "$5.00" или "₽300.00".</summary>
    public string? AmountText { get; init; }

    public long TimestampUsec { get; init; }
}

public enum YouTubeChatPartKind
{
    Text,
    Emote
}

/// <summary>Кусок сообщения: либо текст, либо URL картинки эмодзи.</summary>
public record YouTubeChatPart(YouTubeChatPartKind Kind, string Content);

/// <summary>
///     Бейдж автора. Кастомные бейджи (спонсорство) приходят картинкой (ImageUrl),
///     системные (MODERATOR/OWNER/VERIFIED) — вектором, у них есть только IconType.
/// </summary>
public record YouTubeChatBadge(string? IconType, string? ImageUrl, string Tooltip);
