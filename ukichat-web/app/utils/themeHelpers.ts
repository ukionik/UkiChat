import type { MessageType, ReplyInfo } from "~/types/ChatMessage";

// Визуальный вариант сообщения, общий для всех тем. Реальных типов больше,
// чем в набросках, поэтому сводим их к четырём группам подсветки:
//  - mention      — обращение к стримеру (тревожный акцент);
//  - event        — донат/подписка/рейд/биты/награда (позитивный акцент);
//  - notification  — системное уведомление (нейтральный);
//  - normal       — обычное сообщение (в т.ч. ответ).
export type ThemeVariant = 'mention' | 'event' | 'notification' | 'normal'

export function messageVariant(type: MessageType | undefined): ThemeVariant {
  if (type === 'Mention') return 'mention'
  if (type === 'Donation' || type === 'Subscription' || type === 'Raid'
      || type === 'Cheer' || type === 'ChannelPointsRedemption') return 'event'
  if (type === 'Notification') return 'notification'
  return 'normal'
}


// Общие чистые хелперы для тем чата (без состояния).

// Иконка платформы по её идентификатору.
export function getPlatformImage(platform: string): string {
  switch (platform) {
    case "Twitch": return "/images/twitch.svg"
    case "VkVideoLive": return "/images/vk-video-live.svg"
    case "YouTube": return "/images/youtube.svg"
    case "DonationAlerts": return "/images/donation-alerts.svg"
    default: return ""
  }
}

// Фирменный цвет платформы (для акцентных полос/торцов в темах).
export function getPlatformColor(platform: string): string {
  switch (platform) {
    case "Twitch": return "#9146ff"
    case "VkVideoLive": return "#0077ff"
    case "YouTube": return "#ff0000"
    case "DonationAlerts": return "#f57d07"
    default: return "#888888"
  }
}

// Текст-превью сообщения, на которое отвечают (только текстовые части).
export function getReplyPreview(replyTo: ReplyInfo): string {
  return replyTo.messageParts.filter(p => p.type === 'Text').map(p => p.content).join('').trim()
}
