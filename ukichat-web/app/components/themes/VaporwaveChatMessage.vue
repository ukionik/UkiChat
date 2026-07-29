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

// Градиент почти непрозрачный (0.9), но цвета подобраны так, чтобы над тёмным
// фоном давать тот же оттенок, что прежние розовый/фиолетовый на альфе 0.35.
const boxStyle = computed(() => ({
  padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
  marginBottom: `${0.45 * s.value}rem`,
  borderRadius: `${0.4 * s.value}rem`,
  fontSize: `${s.value}rem`,
  background: 'linear-gradient(135deg, rgba(111, 45, 95, 0.9), rgba(59, 45, 116, 0.9))',
  border: '1px solid rgba(255, 150, 230, 0.5)',
  boxShadow: '0 0 10px rgba(255, 90, 200, 0.3)',
}))

// Подсветка типовых сообщений цветом текста. Оттенки светлые и неоновые —
// тёмный розово-фиолетовый градиент фона съедает насыщенные цвета.
const textColor = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#ffb26b'
  if (variant === 'mention') return '#ff7a8f'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#d6a3ff'
  if (variant === 'event') return '#aaffea'
  return 'inherit'
})

// Ник: тёмная обводка даёт контраст с градиентом, широкое мягкое свечение
// сохраняет ретро-вайб, но уже не размывает сами буквы.
const nameStyle = computed(() => ({
  color: props.message.displayNameColor,
  letterSpacing: '0.5px',
  // Отступ между ником с двоеточием и текстом сообщения
  marginRight: `${0.25 * s.value}rem`,
  textShadow: `0 0 ${0.06 * s.value}rem rgba(20, 8, 30, 0.95), 0 ${0.05 * s.value}rem ${0.1 * s.value}rem rgba(20, 8, 30, 0.7), 0 0 ${0.55 * s.value}rem rgba(255, 255, 255, 0.35)`,
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" dono-class="text-[#aaffea] font-bold" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.15" inline />
      <span class="font-extrabold align-middle" :style="nameStyle">{{ message.displayName }}:</span>
      <span class="align-middle" :style="{ color: textColor }">
        <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                            :revealed="revealed" @link-click="emit('linkClick', $event)" />
      </span>
    </div>
  </div>
</template>
