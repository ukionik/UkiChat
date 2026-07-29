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

// Акцент типа: цвет левой полосы, свечения и текста. Палитра — сигнальная,
// как индикаторы состояния на игровом HUD.
const accent = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return { line: '#ffb020', glow: '255,176,32', text: '#ffd79a' }
  if (variant === 'mention') return { line: '#ff3b5c', glow: '255,59,92', text: '#ff9dae' }
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return { line: '#b06bff', glow: '176,107,255', text: '#d9bcff' }
  if (variant === 'event') return { line: '#2bffa3', glow: '43,255,163', text: '#a6ffdb' }
  return { line: '#28d8ff', glow: '40,216,255', text: '#cfefff' }
})

const boxStyle = computed(() => {
  const cut = `${0.5 * s.value}rem`
  return {
    padding: `${0.4 * s.value}rem ${0.7 * s.value}rem`,
    marginBottom: `${0.4 * s.value}rem`,
    fontSize: `${s.value}rem`,
    fontFamily: '"Consolas", "Cascadia Mono", monospace',
    lineHeight: '1.4',
    color: accent.value.text,
    background: 'linear-gradient(100deg, rgba(0, 28, 44, 0.92), rgba(0, 12, 24, 0.88))',
    borderLeft: `${0.2 * s.value}rem solid ${accent.value.line}`,
    boxShadow: `inset 0 0 ${0.8 * s.value}rem rgba(${accent.value.glow}, 0.16), 0 0 ${0.5 * s.value}rem rgba(${accent.value.glow}, 0.25)`,
    // Срезанные углы справа — узнаваемая форма интерфейсных панелей.
    clipPath: `polygon(0 0, calc(100% - ${cut}) 0, 100% ${cut}, 100% 100%, ${cut} 100%, 0 calc(100% - ${cut}))`,
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#7fa6bd]" dono-class="text-[#2bffa3] font-bold"
                     reward-class="text-[#b06bff]" bits-class="text-[#28d8ff]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" inline />
      <span class="opacity-50">//</span>
      <span class="font-bold align-middle" :style="{ marginRight, marginLeft: marginRight, color: message.displayNameColor,
            textShadow: `0 0 ${0.4 * s}rem rgba(${accent.glow}, 0.5)` }">{{ message.displayName }}</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
