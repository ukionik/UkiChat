import type { ChatMessage, MessagePart, MessageType } from "~/types/ChatMessage";

// Генератор случайных сообщений для превью темы в настройках.
// Не ходит в сеть: эмоуты — это инлайновые SVG c эмодзи (data URI),
// бейджи — локальные SVG из public/images.

const platforms = ['Twitch', 'VkVideoLive', 'YouTube', 'DonationAlerts']

const names = [
  'NightBot', 'Аня_Котик', 'GamerPro2010', 'xX_Slayer_Xx', 'Светлана',
  'pog_champ', 'Дмитрий', 'StreamFan99', 'Лиса', 'mrBeastik',
  'TheLegend27', 'Котейка', 'darksoul', 'Машенька', 'ProPlayer',
]

const colors = [
  '#ff4d4d', '#4dff88', '#4d9bff', '#ffd24d', '#ff79c6',
  '#bd93f9', '#8be9fd', '#ffb86c', '#50fa7b', '#ff5555',
]

const texts = [
  'Привет всем на стриме!',
  'это просто лучший контент',
  'го рейд на соседний канал',
  'кто тут первый раз?',
  'ору с этого момента',
  'красавчик, так держать',
  'а во сколько следующий стрим?',
  'POG какой клатч',
  'спасибо за стрим, было круто',
  'ну ты конечно красава',
  'хахаха это топ',
  'давай ещё одну катку!',
]

// Локальные бейджи (есть в public/images)
const badgePool = [
  '/images/youtube/moderator.svg',
  '/images/youtube/verified.svg',
  '/images/channel-points.svg',
]

// Эмоут как инлайновый SVG с эмодзи — отрисуется как обычная картинка-эмоут.
function emote(emoji: string): string {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32"><text x="16" y="26" font-size="26" text-anchor="middle">${emoji}</text></svg>`
  return `data:image/svg+xml,${encodeURIComponent(svg)}`
}

const emotes = ['🎉', '😂', '❤️', '🚀', '🔥', '😎', '👀', '💀', '🐸', '✨'].map(emote)

function pick<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)] as T
}

function maybe(probability: number): boolean {
  return Math.random() < probability
}

// Собираем messageParts: текст + иногда эмоут в конце/середине.
function buildParts(): MessagePart[] {
  const parts: MessagePart[] = [{ type: 'Text', content: pick(texts) + ' ' }]
  if (maybe(0.5)) parts.push({ type: 'Emote', content: pick(emotes) })
  return parts
}

// Веса типов сообщений: в основном обычные, спец-события реже.
const typeWeights: { type: MessageType; weight: number }[] = [
  { type: 'Normal', weight: 60 },
  { type: 'Mention', weight: 8 },
  { type: 'Reply', weight: 8 },
  { type: 'Donation', weight: 7 },
  { type: 'Subscription', weight: 5 },
  { type: 'Raid', weight: 4 },
  { type: 'Cheer', weight: 4 },
  { type: 'ChannelPointsRedemption', weight: 4 },
]

function pickType(): MessageType {
  const total = typeWeights.reduce((s, t) => s + t.weight, 0)
  let r = Math.random() * total
  for (const t of typeWeights) {
    if ((r -= t.weight) < 0) return t.type
  }
  return 'Normal'
}

let counter = 0

// Создаёт одно случайное сообщение для превью.
export function createSampleMessage(): ChatMessage {
  const name = pick(names)
  const color = pick(colors)
  const type = pickType()

  const badges: string[] = []
  if (maybe(0.4)) badges.push(pick(badgePool))

  const msg: ChatMessage = {
    id: `preview-${Date.now()}-${counter++}`,
    platform: pick(platforms),
    badges,
    displayName: name,
    displayNameColor: color,
    messageParts: buildParts(),
    messageType: type,
  }

  if (type === 'Mention') {
    msg.messageParts = [{ type: 'Text', content: '@стример ' + pick(texts) }]
  } else if (type === 'Reply') {
    msg.replyTo = {
      displayName: pick(names),
      displayNameColor: pick(colors),
      messageParts: [{ type: 'Text', content: pick(texts) }],
    }
  } else if (type === 'Donation') {
    msg.donationAmount = `${pick([100, 250, 500, 1000])} ₽`
  } else if (type === 'Cheer') {
    msg.bits = pick([100, 500, 1000, 5000])
  } else if (type === 'ChannelPointsRedemption') {
    msg.rewardTitle = pick(['Выделить сообщение', 'Сменить трек', 'Привет на стриме'])
    msg.rewardCost = pick([500, 1000, 2000])
  }

  return msg
}
