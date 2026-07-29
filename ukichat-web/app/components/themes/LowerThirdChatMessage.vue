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
const gap = computed(() => `${0.3 * s.value}rem`)
const nameFontSize = computed(() => `${0.85 * s.value}rem`)

// Подсветка типовых сообщений цветом текста — тёмная плашка, оттенки яркие.
const textColor = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#ffb266'
  if (variant === 'mention') return '#ff6b7a'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#c58bff'
  if (variant === 'event') return '#69f0ae'
  return 'inherit'
})
</script>

<template>
  <div
    class="flex items-stretch overflow-hidden break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="{ marginBottom: `${0.45 * s}rem`, borderRadius: `${0.25 * s}rem`, fontSize: `${s}rem`, background: 'linear-gradient(90deg, rgba(0,0,0,0.9), rgba(0,0,0,0.85))' }"
    @click="toggleRevealDeleted"
  >
    <div class="shrink-0" :style="{ width: `${0.35 * s}rem`, background: getPlatformColor(message.platform) }"></div>
    <div :style="{ padding: `${0.45 * s}rem ${0.75 * s}rem` }">
      <ChatMessageMeta :message="message" :scale="scale" dono-class="text-[#69f0ae] font-bold" />
      <div class="flex items-center" :style="{ gap, marginBottom: gap }">
        <span class="font-extrabold uppercase leading-none" :style="{ color: message.displayNameColor, fontSize: nameFontSize, letterSpacing: '0.5px' }">{{ message.displayName }}</span>
        <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="0.95" />
      </div>
      <div class="leading-snug" :style="{ color: textColor }">
        <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                            :revealed="revealed" @link-click="emit('linkClick', $event)" />
      </div>
    </div>
  </div>
</template>
