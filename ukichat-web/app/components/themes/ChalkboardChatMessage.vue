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
const marginRight = computed(() => `${0.3 * s.value}rem`)

// Цвет мелка по типу сообщения — пастельная гамма школьного набора.
const chalk = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#ffd08a'
  if (variant === 'mention') return '#ff9f9f'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#d5b3ff'
  if (variant === 'event') return '#b6f2a8'
  return '#f2f0e6'
})

const boxStyle = computed(() => ({
  padding: `${0.4 * s.value}rem ${0.75 * s.value}rem`,
  marginBottom: `${0.3 * s.value}rem`,
  fontSize: `${s.value}rem`,
  fontFamily: '"Segoe Print", "Comic Sans MS", "Bradley Hand", cursive',
  lineHeight: '1.45',
  color: chalk.value,
  // Неровная засветка сверху имитирует затёртую грифельную доску.
  background: 'radial-gradient(120% 90% at 25% 0%, rgba(255, 255, 255, 0.07), rgba(255, 255, 255, 0) 60%), #22332a',
  borderBottom: '1px solid rgba(255, 255, 255, 0.08)',
  // Меловой штрих: буквы слегка «пылят» по краям.
  textShadow: `0 0 ${0.09 * s.value}rem rgba(255, 255, 255, 0.45)`,
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#b9c9bd]" dono-class="text-[#b6f2a8] font-bold"
                     reward-class="text-[#d5b3ff]" bits-class="text-[#a8e4f2]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" inline />
      <span class="font-bold align-middle" :style="{ marginRight, color: message.displayNameColor }">{{ message.displayName }}:</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
