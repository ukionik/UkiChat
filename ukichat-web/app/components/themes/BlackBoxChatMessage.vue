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

// Подсветка типовых сообщений глубокой тонировкой фона: тема строится на почти
// чёрных плашках, поэтому цвет добавляется и в рамку — иначе тона неразличимы.
const accent = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid')
    return { background: 'rgba(42, 24, 0, 0.86)', border: 'rgba(255, 170, 60, 0.22)' }
  if (variant === 'mention')
    return { background: 'rgba(40, 0, 0, 0.86)', border: 'rgba(255, 80, 80, 0.22)' }
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer')
    return { background: 'rgba(28, 0, 45, 0.86)', border: 'rgba(180, 110, 255, 0.22)' }
  if (variant === 'event')
    return { background: 'rgba(0, 30, 12, 0.86)', border: 'rgba(80, 230, 140, 0.22)' }
  return { background: 'rgba(0, 0, 0, 0.9)', border: 'rgba(255, 255, 255, 0.07)' }
})

const boxStyle = computed(() => ({
  padding: `${0.3 * s.value}rem ${0.7 * s.value}rem`,
  marginBottom: `${0.4 * s.value}rem`,
  borderRadius: `${0.7 * s.value}rem`,
  fontSize: `${s.value}rem`,
  lineHeight: '1.4',
  border: `1px solid ${accent.value.border}`,
  background: accent.value.background,
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.15" inline />
      <span class="font-extrabold align-middle" :style="{ color: message.displayNameColor }">{{ message.displayName }}:</span>
      <span class="align-middle"> </span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
