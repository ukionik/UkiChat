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
    padding: `${0.4 * s.value}rem ${0.7 * s.value}rem`,
    marginBottom: `${0.5 * s.value}rem`,
    borderRadius: `${0.9 * s.value}rem`,
    fontSize: `${s.value}rem`,
    color: '#1a1a1a',
    border: `${0.18 * s.value}rem solid #1a1a1a`,
    boxShadow: `${0.2 * s.value}rem ${0.2 * s.value}rem 0 #1a1a1a`,
    background: '#fffbe6',
  }
  const variant = messageVariant(props.message.messageType)
  if (variant === 'mention') base.background = '#ffd9d9'
  else if (variant === 'event') base.background = '#d9ffe1'
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-gray-600" dono-class="text-green-700 font-extrabold"
                     reward-class="text-purple-700" bits-class="text-cyan-700" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.15" inline />
      <span class="font-extrabold align-middle" :style="{ color: message.displayNameColor }">{{ message.displayName }}:</span>
      <span class="align-middle"> </span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
