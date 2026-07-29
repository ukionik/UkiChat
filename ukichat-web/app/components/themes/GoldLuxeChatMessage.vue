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
const marginRight = computed(() => `${0.35 * s.value}rem`)

// Акцент типа сообщения в «ювелирной» гамме: бронза, рубин, аметист, изумруд.
// Базовый — золото, оно же задаёт общий тон темы.
const accent = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return { line: '#cd7f32', gradient: ['#f0c48a', '#cd7f32', '#8c5420'] }
  if (variant === 'mention') return { line: '#d4485f', gradient: ['#f3a0ad', '#d4485f', '#912a3c'] }
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return { line: '#a06cd5', gradient: ['#d9bff5', '#a06cd5', '#6a3fa0'] }
  if (variant === 'event') return { line: '#3fae7a', gradient: ['#9ce6c2', '#3fae7a', '#22754e'] }
  return { line: '#d4af37', gradient: ['#f7e7a3', '#d4af37', '#a67c00'] }
})

const boxStyle = computed(() => ({
  padding: `${0.45 * s.value}rem ${0.8 * s.value}rem`,
  marginBottom: `${0.45 * s.value}rem`,
  borderRadius: `${0.15 * s.value}rem`,
  fontSize: `${s.value}rem`,
  fontFamily: 'Georgia, "Times New Roman", serif',
  lineHeight: '1.45',
  color: '#efe6d2',
  background: 'linear-gradient(160deg, rgba(22, 18, 12, 0.94), rgba(8, 6, 4, 0.96))',
  border: `1px solid ${accent.value.line}`,
  boxShadow: `inset 0 0 0 1px rgba(255, 255, 255, 0.05), 0 ${0.1 * s.value}rem ${0.5 * s.value}rem rgba(0, 0, 0, 0.55)`,
}))

// Ник залит металлическим градиентом — главная деталь темы, поэтому цвет
// платформы здесь уступает место «фирменному» блеску.
const nameStyle = computed(() => {
  const [light, mid, dark] = accent.value.gradient
  return {
    marginRight: marginRight.value,
    backgroundImage: `linear-gradient(180deg, ${light} 0%, ${mid} 55%, ${dark} 100%)`,
    backgroundClip: 'text',
    WebkitBackgroundClip: 'text',
    color: 'transparent',
    letterSpacing: '0.03em',
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#9c9280] italic" dono-class="text-[#3fae7a] font-bold"
                     reward-class="text-[#a06cd5]" bits-class="text-[#7fb8d4]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" inline />
      <span class="font-bold align-middle" :style="nameStyle">{{ message.displayName }}</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
