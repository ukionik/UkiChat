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

const boxStyle = computed(() => {
  const base: Record<string, string> = {
    padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
    marginBottom: `${0.4 * s.value}rem`,
    borderRadius: `${0.9 * s.value}rem`,
    fontSize: `${s.value}rem`,
    color: '#5a2a45',
    background: 'linear-gradient(135deg, #ffe3f1, #ffd0e6)',
    boxShadow: '0 1px 5px rgba(255, 150, 200, 0.45)',
  }
  const variant = messageVariant(props.message.messageType)
  if (variant === 'mention') base.background = 'linear-gradient(135deg, #ffd4dc, #ffc2da)'
  else if (variant === 'event') base.background = 'linear-gradient(135deg, #ffdcf3, #ffc9ec)'
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#a05278]" dono-class="text-[#d6337a] font-bold"
                     reward-class="text-[#a05278]" bits-class="text-[#a05278]" />
    <div class="flex items-center" :style="{ gap, marginBottom: gap }">
      <span class="text-[#ff6fb0]">♥</span>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" />
      <span class="font-extrabold leading-none" :style="{ color: message.displayNameColor }">{{ message.displayName }}</span>
    </div>
    <div class="leading-snug">
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
