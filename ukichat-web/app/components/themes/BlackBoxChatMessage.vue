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

const boxStyle = computed(() => {
  const base: Record<string, string> = {
    padding: `${0.3 * s.value}rem ${0.7 * s.value}rem`,
    marginBottom: `${0.4 * s.value}rem`,
    borderRadius: `${0.7 * s.value}rem`,
    fontSize: `${s.value}rem`,
    lineHeight: '1.4',
    border: '1px solid rgba(255, 255, 255, 0.07)',
    background: 'rgba(0, 0, 0, 0.86)',
  }
  const variant = messageVariant(props.message.messageType)
  if (variant === 'mention') base.background = 'rgba(40, 0, 0, 0.86)'
  else if (variant === 'event') base.background = 'rgba(0, 30, 12, 0.86)'
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
      <span class="font-extrabold align-middle" :style="{ color: message.displayNameColor }">{{ message.displayName }}:</span>
      <span class="align-middle"> </span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
