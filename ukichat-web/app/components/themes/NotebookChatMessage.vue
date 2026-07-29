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
// Отступ между ником с двоеточием и текстом записи
const marginRight = computed(() => `${0.3 * s.value}rem`)

// Цвет чернил по типу сообщения: обычные записи синей ручкой, события —
// цветной пастой, будто пометки на полях.
const ink = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#b45309'
  if (variant === 'mention') return '#b91c1c'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#6d28d9'
  if (variant === 'event') return '#15803d'
  return '#1b3a8a'
})

const boxStyle = computed(() => {
  const line = 1.45 * s.value
  return {
    padding: `${0.3 * s.value}rem ${0.7 * s.value}rem ${0.3 * s.value}rem ${1.15 * s.value}rem`,
    fontSize: `${s.value}rem`,
    lineHeight: `${line}rem`,
    color: ink.value,
    // Тетрадный лист: горизонтальная линовка + красная линия поля слева.
    // Шаг линовки совпадает с line-height, иначе текст «съезжает» с линеек.
    background: `linear-gradient(90deg, transparent 0 ${0.85 * s.value}rem, rgba(216, 96, 96, 0.55) ${0.85 * s.value}rem ${0.92 * s.value}rem, transparent ${0.92 * s.value}rem),
                 repeating-linear-gradient(#fdfdf6 0 ${line - 0.06}rem, #c9d6e8 ${line - 0.06}rem ${line}rem)`,
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#6b7a93] italic" dono-class="text-[#15803d] font-bold"
                     reward-class="text-[#6d28d9]" bits-class="text-[#0e7490]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" inline />
      <span class="font-bold align-middle" :style="{ marginRight, color: message.displayNameColor,
            textShadow: '0 0 0.05rem rgba(27, 58, 138, 0.5)' }">{{ message.displayName }}:</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
