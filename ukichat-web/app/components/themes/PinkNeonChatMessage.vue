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
    padding: `${0.45 * s.value}rem ${0.6 * s.value}rem`,
    marginBottom: `${0.45 * s.value}rem`,
    borderRadius: `${0.5 * s.value}rem`,
    fontSize: `${s.value}rem`,
    background: 'rgba(35, 8, 28, 0.7)',
    border: '1px solid #ff4dd2',
    boxShadow: '0 0 9px rgba(255,77,210,0.55), inset 0 0 6px rgba(255,77,210,0.15)',
  }
  const variant = messageVariant(props.message.messageType)
  if (variant === 'mention') {
    base.border = '1px solid #ff4d7a'
    base.boxShadow = '0 0 9px rgba(255,77,122,0.6)'
  } else if (variant === 'event') {
    base.border = '1px solid #ff8adf'
  }
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#ffb3ec]" dono-class="text-[#ffb3ec] font-bold"
                     reward-class="text-[#ffb3ec]" bits-class="text-[#ffb3ec]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.15" inline />
      <span class="font-extrabold align-middle text-[#ff9ee0]" :style="{ textShadow: '0 0 7px #ff4dd2' }">{{ message.displayName }}:</span>
      <span class="align-middle"> </span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
