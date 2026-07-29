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
// Отступ между ником и текстом заметки
const marginRight = computed(() => `${0.3 * s.value}rem`)

// Акцент типа сообщения: цвет верхней линейки и ника. Гамма типографская —
// приглушённые краски, какими печатают заголовки рубрик.
const accent = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#a35a12'
  if (variant === 'mention') return '#a62121'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#5b2d91'
  if (variant === 'event') return '#1a6b3c'
  return '#241f16'
})

const boxStyle = computed(() => ({
  padding: `${0.4 * s.value}rem ${0.7 * s.value}rem ${0.45 * s.value}rem`,
  marginBottom: `${0.4 * s.value}rem`,
  fontSize: `${s.value}rem`,
  fontFamily: 'Georgia, "Times New Roman", serif',
  lineHeight: '1.45',
  color: '#241f16',
  background: '#f4ecd8',
  borderTop: `${0.15 * s.value}rem solid ${accent.value}`,
  borderBottom: '1px solid rgba(36, 31, 22, 0.3)',
}))

// Ник набран как имя автора заметки: капсом, с разрядкой.
const nameStyle = computed(() => ({
  marginRight: marginRight.value,
  color: accent.value,
  letterSpacing: '0.06em',
  fontSize: `${0.9 * s.value}rem`,
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#6b6152] italic" dono-class="text-[#1a6b3c] font-bold"
                     reward-class="text-[#5b2d91]" bits-class="text-[#1f5f7a]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" inline />
      <span class="font-bold uppercase align-middle" :style="nameStyle">{{ message.displayName }}</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
