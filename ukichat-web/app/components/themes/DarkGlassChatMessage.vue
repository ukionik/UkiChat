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
const gap = computed(() => `${0.35 * s.value}rem`)
const nameFontSize = computed(() => `${0.95 * s.value}rem`)

// Цвет рамки по типу сообщения. Тема тёмно-стеклянная, поэтому акценты держим
// в двух её цветах: белый — нейтральные события (уведомление, рейд),
// фиолетовый — «твичевые» платные/наградные (баллы, подписка, биты).
// Донат остаётся зелёным, обращение к стримеру — красным.
const BORDER_COLORS: Record<string, string> = {
  Notification: 'rgba(255, 255, 255, 0.7)',
  Raid: 'rgba(255, 255, 255, 0.7)',
  ChannelPointsRedemption: 'rgba(145, 70, 255, 0.8)',
  Subscription: 'rgba(145, 70, 255, 0.8)',
  Cheer: 'rgba(145, 70, 255, 0.8)',
  Donation: 'rgba(46, 204, 113, 0.7)',
  Mention: 'rgba(231, 76, 60, 0.7)',
}

const boxStyle = computed(() => {
  const base: Record<string, string> = {
    padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
    borderRadius: `${0.5 * s.value}rem`,
    marginBottom: `${0.4 * s.value}rem`,
    fontSize: `${s.value}rem`,
    background: 'rgba(0, 0, 0, 0.85)',
    border: '1px solid rgba(255, 255, 255, 0.14)',
    backdropFilter: 'blur(3px)',
  }
  const accent = props.message.messageType ? BORDER_COLORS[props.message.messageType] : undefined
  if (accent) {
    base.border = `1px solid ${accent}`
  }
  if (props.message.messageType === 'Mention') {
    base.boxShadow = 'inset 0 0 0 1px rgba(231, 76, 60, 0.4)'
  }
  return base
})
</script>

<template>
  <div
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" with-margin />
    <div class="flex items-center" :style="{ gap, marginBottom: gap }">
      <ChatPlatformBadges :message="message" :scale="scale" />
      <span class="font-bold leading-none"
            :style="{ color: message.displayNameColor, fontSize: nameFontSize }">{{ message.displayName }}</span>
    </div>
    <div class="leading-snug break-words">
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
