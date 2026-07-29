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
// Отступ между ником с двоеточием и текстом сообщения
const marginRight = computed(() => `${0.25 * s.value}rem`)

// Подсветка типовых сообщений неоновой рамкой со свечением плюс цвет текста.
// Гамма остаётся розово-неоновой (как в Pink Soft), меняется тон: коралловый,
// розово-красный, лиловый и малиновый.
const accent = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid')
    return { border: '#ff8a5c', glow: '255,138,92', text: '#ffc9a3' }
  if (variant === 'mention')
    return { border: '#ff4d7a', glow: '255,77,122', text: '#ff9aae' }
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer')
    return { border: '#c14dff', glow: '193,77,255', text: '#dcb0ff' }
  if (variant === 'event')
    return { border: '#ff2e9f', glow: '255,46,159', text: '#ffa3d5' }
  return { border: '#ff4dd2', glow: '255,77,210', text: 'inherit' }
})

const boxStyle = computed(() => ({
  padding: `${0.45 * s.value}rem ${0.6 * s.value}rem`,
  marginBottom: `${0.45 * s.value}rem`,
  borderRadius: `${0.5 * s.value}rem`,
  fontSize: `${s.value}rem`,
  background: 'rgba(35, 8, 28, 0.9)',
  border: `1px solid ${accent.value.border}`,
  boxShadow: `0 0 9px rgba(${accent.value.glow},0.55), inset 0 0 6px rgba(${accent.value.glow},0.15)`,
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#ffb3ec]" dono-class="text-[#ffb3ec] font-bold"
                     reward-class="text-[#ffb3ec]" bits-class="text-[#ffb3ec]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.15" inline />
      <span class="font-extrabold align-middle text-[#ff9ee0]" :style="{ marginRight, textShadow: '0 0 7px #ff4dd2' }">{{ message.displayName }}:</span>
      <span class="align-middle" :style="{ color: accent.text }">
        <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                            :revealed="revealed" @link-click="emit('linkClick', $event)" />
      </span>
    </div>
  </div>
</template>
