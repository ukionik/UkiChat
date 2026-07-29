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

// Подсветка типовых сообщений тонировкой стекла и цветом рамки — светлые
// оттенки, чтобы сквозь полупрозрачную заливку цвет оставался узнаваемым.
function accentColor(message: ChatMessage): string {
  const type = message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '255, 176, 80'
  if (variant === 'mention') return '255, 90, 100'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '190, 130, 255'
  if (variant === 'event') return '90, 230, 160'
  return '255, 255, 255'
}

const boxStyle = computed(() => {
  const accent = accentColor(props.message)
  const tinted = accent !== '255, 255, 255'
  return {
    padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
    marginBottom: `${0.4 * s.value}rem`,
    borderRadius: `${0.6 * s.value}rem`,
    fontSize: `${s.value}rem`,
    background: `rgba(${accent}, ${tinted ? 0.16 : 0.1})`,
    border: `1px solid rgba(${accent}, ${tinted ? 0.5 : 0.28})`,
    backdropFilter: 'blur(6px)',
    boxShadow: '0 2px 8px rgba(0, 0, 0, 0.2)',
    // Заливка почти прозрачная, поэтому читаемость держится на тени текста.
    textShadow: `0 ${0.05 * s.value}rem ${0.14 * s.value}rem rgba(0, 0, 0, 0.9), 0 0 ${0.08 * s.value}rem rgba(0, 0, 0, 0.75)`,
  }
})
</script>

<template>
  <div
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" with-margin reply-class="text-gray-200" />
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
