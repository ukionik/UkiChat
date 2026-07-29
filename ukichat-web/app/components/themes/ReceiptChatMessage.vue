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

// Акцент типа — цвет пунктирной линии и ника. Бумага светлая, поэтому
// оттенки тёмные, как у чекового принтера с цветной лентой.
const accent = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#b45309'
  if (variant === 'mention') return '#b91c1c'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#7e22ce'
  if (variant === 'event') return '#15803d'
  return '#22201c'
})

const boxStyle = computed(() => {
  const tooth = `${0.55 * s.value}rem`
  // Зубчатый нижний край: маска вырезает ряд полукругов по нижней кромке —
  // так же, как чек отрывается от рулона.
  const mask = `radial-gradient(circle at 50% 100%, transparent 0 ${0.22 * s.value}rem, #000 ${0.22 * s.value}rem)`
  return {
    padding: `${0.45 * s.value}rem ${0.75 * s.value}rem ${0.7 * s.value}rem`,
    marginBottom: `${0.35 * s.value}rem`,
    fontSize: `${0.95 * s.value}rem`,
    fontFamily: '"Consolas", "Courier New", monospace',
    lineHeight: '1.45',
    color: '#22201c',
    background: '#fbfaf5',
    boxShadow: `0 ${0.1 * s.value}rem ${0.35 * s.value}rem rgba(0, 0, 0, 0.35)`,
    maskImage: mask,
    maskSize: `${tooth} 100%`,
    maskRepeat: 'repeat-x',
    WebkitMaskImage: mask,
    WebkitMaskSize: `${tooth} 100%`,
    WebkitMaskRepeat: 'repeat-x',
  }
})

// Шапка чека отделена пунктиром — «линией отрыва» под реквизитами.
const headStyle = computed(() => ({
  marginBottom: `${0.3 * s.value}rem`,
  paddingBottom: `${0.25 * s.value}rem`,
  borderBottom: `1px dashed ${accent.value}`,
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <div class="flex items-center" :style="[headStyle, { gap: marginRight }]">
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1" />
      <span class="font-bold uppercase leading-none" :style="{ color: accent, letterSpacing: '0.08em',
            fontSize: `${0.85 * s}rem` }">{{ message.displayName }}</span>
    </div>
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#8a857a]" dono-class="text-[#15803d] font-bold"
                     reward-class="text-[#7e22ce]" bits-class="text-[#0e7490]" />
    <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                        :revealed="revealed" @link-click="emit('linkClick', $event)" />
  </div>
</template>
