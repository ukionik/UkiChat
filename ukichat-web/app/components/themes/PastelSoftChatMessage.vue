<script setup lang="ts">
import type { ChatMessage } from "~/types/ChatMessage";

const props = defineProps<{
  message: ChatMessage
  scale: number
  allowRevealDeleted: boolean
}>()

const emit = defineEmits<{
  linkClick: [url: string]
}>()

const { revealed, toggleRevealDeleted } = useThemeMessage(props, emit)

const s = computed(() => props.scale)
const gap = computed(() => `${0.3 * s.value}rem`)

// Пастельная заливка по платформе.
function pastelBg(platform: string): string {
  switch (platform) {
    case "Twitch": return "#ece3ff"
    case "VkVideoLive": return "#e3f0ff"
    case "YouTube": return "#ffe3e3"
    case "DonationAlerts": return "#fff0db"
    default: return "#eceffc"
  }
}

// Подсветка типовых сообщений цветом текста (фон остаётся пастельным,
// поэтому оттенки взяты насыщенные и тёмные — для читаемости).
function typeColor(message: ChatMessage): string {
  const type = message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#b45309'
  if (variant === 'mention') return '#b91c1c'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#7e22ce'
  if (variant === 'event') return '#15803d'
  return '#2b2b3a'
}

const boxStyle = computed(() => ({
  padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
  marginBottom: `${0.4 * s.value}rem`,
  borderRadius: `${0.9 * s.value}rem`,
  fontSize: `${s.value}rem`,
  color: typeColor(props.message),
  background: pastelBg(props.message.platform),
}))

// Мягкая тёмная обводка ника, чтобы светлые цвета не сливались с пастельным фоном.
const nameStyle = computed(() => {
  const w = `${0.05 * s.value}rem`
  return {
    color: props.message.displayNameColor,
    textShadow: `0 0 ${w} rgba(43, 43, 58, 0.72), 0 0 ${0.12 * s.value}rem rgba(43, 43, 58, 0.38)`,
  }
})
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-gray-500" dono-class="text-[#1b8a4b] font-bold"
                     reward-class="text-purple-700" bits-class="text-cyan-700" />
    <div class="flex items-center" :style="{ gap, marginBottom: gap }">
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" />
      <span class="font-bold leading-none" :style="nameStyle">{{ message.displayName }}</span>
    </div>
    <div class="leading-snug">
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
