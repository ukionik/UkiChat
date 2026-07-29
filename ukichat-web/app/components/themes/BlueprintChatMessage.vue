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
// Отступ между ником и текстом сообщения
const marginRight = computed(() => `${0.3 * s.value}rem`)

// Акцент типа — цвет рамки и ника, как пометка другим карандашом на чертеже.
const accent = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#ffc46b'
  if (variant === 'mention') return '#ff8a8a'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#c9a3ff'
  if (variant === 'event') return '#7dffc0'
  return '#a8d8ff'
})

const boxStyle = computed(() => {
  const cell = `${0.7 * s.value}rem`
  return {
    padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
    marginBottom: `${0.4 * s.value}rem`,
    fontSize: `${0.95 * s.value}rem`,
    fontFamily: '"Consolas", "Cascadia Mono", monospace',
    lineHeight: '1.45',
    color: '#dceeff',
    // Миллиметровка: две повторяющиеся линовки поверх густой синей заливки.
    background: `repeating-linear-gradient(90deg, rgba(168, 216, 255, 0.13) 0 1px, transparent 1px ${cell}),
                 repeating-linear-gradient(rgba(168, 216, 255, 0.13) 0 1px, transparent 1px ${cell}),
                 rgba(11, 52, 92, 0.92)`,
    border: `1px solid ${accent.value}`,
    boxShadow: `inset 0 0 ${0.6 * s.value}rem rgba(0, 0, 0, 0.35)`,
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#8fb6d9]" dono-class="text-[#7dffc0] font-bold"
                     reward-class="text-[#c9a3ff]" bits-class="text-[#a8d8ff]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" inline />
      <span class="font-bold uppercase align-middle" :style="{ marginRight, color: accent, letterSpacing: '0.05em',
            fontSize: `${0.9 * s}rem` }">{{ message.displayName }}</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
