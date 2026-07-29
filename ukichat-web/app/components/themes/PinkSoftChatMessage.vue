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

const heartSize = computed(() => `${1.05 * s.value}rem`)

// Подсветка типовых сообщений остаётся в розовой гамме — меняется только тон:
// персиковый, розово-красный, лиловый и малиновый. Сердечко берёт насыщенный
// вариант того же тона, иначе близкие пастельные заливки трудно различить.
const accent = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid')
    return { background: 'linear-gradient(135deg, #ffe4d6, #ffd0bd)', heart: '#ff8a5c' }
  if (variant === 'mention')
    return { background: 'linear-gradient(135deg, #ffd4dc, #ffbfcd)', heart: '#f4416b' }
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer')
    return { background: 'linear-gradient(135deg, #f4daff, #e6c4fb)', heart: '#b45cf0' }
  if (variant === 'event')
    return { background: 'linear-gradient(135deg, #ffdcf3, #ffc9ec)', heart: '#e83fa8' }
  return { background: 'linear-gradient(135deg, #ffe3f1, #ffd0e6)', heart: '#ff6fb0' }
})

const boxStyle = computed(() => ({
  padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
  marginBottom: `${0.4 * s.value}rem`,
  borderRadius: `${0.9 * s.value}rem`,
  fontSize: `${s.value}rem`,
  color: '#5a2a45',
  background: accent.value.background,
  boxShadow: '0 1px 5px rgba(255, 150, 200, 0.45)',
}))

// Обводка ника в тон темы — светлые цвета ников тонут в пастельной заливке.
const nameStyle = computed(() => {
  const w = `${0.05 * s.value}rem`
  return {
    color: props.message.displayNameColor,
    textShadow: `0 0 ${w} rgba(90, 42, 69, 0.72), 0 0 ${0.12 * s.value}rem rgba(90, 42, 69, 0.38)`,
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#a05278]" dono-class="text-[#d6337a] font-bold"
                     reward-class="text-[#a05278]" bits-class="text-[#a05278]" />
    <div class="flex items-center" :style="{ gap, marginBottom: gap }">
      <svg class="shrink-0" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"
           :style="{ width: heartSize, height: heartSize, color: accent.heart }">
        <path d="M12 20.6l-1.34-1.22C5.9 15.07 2.75 12.2 2.75 8.69 2.75 5.82 5 3.6 7.87 3.6c1.62 0 3.18.75 4.13 1.94.95-1.19 2.51-1.94 4.13-1.94 2.87 0 5.12 2.22 5.12 5.09 0 3.51-3.15 6.38-7.91 10.69L12 20.6z"/>
      </svg>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" />
      <span class="font-extrabold leading-none" :style="nameStyle">{{ message.displayName }}</span>
    </div>
    <div class="leading-snug">
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
