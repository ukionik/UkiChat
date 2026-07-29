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
const marginRight = computed(() => `${0.25 * s.value}rem`)

const boxStyle = computed(() => {
  const base: Record<string, string> = {
    padding: `${0.3 * s.value}rem ${0.65 * s.value}rem`,
    marginBottom: `${0.32 * s.value}rem`,
    fontSize: `${s.value}rem`,
    background: 'rgba(0, 0, 0, 0.9)',
    borderLeft: `${0.25 * s.value}rem solid ${getPlatformColor(props.message.platform)}`,
    borderRadius: `0 ${0.4 * s.value}rem ${0.4 * s.value}rem 0`,
  }
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (variant === 'mention') base.background = 'rgb(113 11 5 / 0.9)'
  else if (type === 'Notification' || type === 'Raid') base.background = 'rgba(94, 62, 0, 0.9)'
  else if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') base.background = 'rgb(66 13 89 / 0.9)'
  else if (variant === 'event') base.background = 'rgb(0 81 35 / 0.9)'
  return base
})
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
      <span class="font-bold align-middle" :style="{ marginRight, color: message.displayNameColor }">{{ message.displayName }}:</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
