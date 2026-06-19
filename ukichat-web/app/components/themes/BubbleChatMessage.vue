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
const avatarSize = computed(() => `${2 * s.value}rem`)
const avatarFont = computed(() => `${0.9 * s.value}rem`)
const gap = computed(() => `${0.4 * s.value}rem`)
const headGap = computed(() => `${0.3 * s.value}rem`)
const nameFontSize = computed(() => `${0.9 * s.value}rem`)
const initial = computed(() => props.message.displayName.charAt(0))

const bubbleStyle = computed(() => {
  const base: Record<string, string> = {
    padding: `${0.4 * s.value}rem ${0.65 * s.value}rem`,
    borderRadius: `${0.25 * s.value}rem ${0.75 * s.value}rem ${0.75 * s.value}rem ${0.75 * s.value}rem`,
    fontSize: `${s.value}rem`,
    background: 'rgba(255, 255, 255, 0.12)',
  }
  const variant = messageVariant(props.message.messageType)
  if (variant === 'mention') base.background = 'rgba(231, 76, 60, 0.25)'
  else if (variant === 'event') base.background = 'rgba(46, 204, 113, 0.25)'
  return base
})
</script>

<template>
  <div
    class="flex items-start"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="{ gap, marginBottom: `${0.5 * s}rem` }"
    @click="toggleRevealDeleted"
  >
    <span class="rounded-full inline-flex items-center justify-center font-extrabold text-white shrink-0"
          :style="{ background: message.displayNameColor, width: avatarSize, height: avatarSize, fontSize: avatarFont }">{{ initial }}</span>
    <div class="min-w-0 break-words" :style="bubbleStyle">
      <div class="flex items-center" :style="{ gap: headGap }">
        <span class="font-bold leading-none" :style="{ color: message.displayNameColor, fontSize: nameFontSize }">{{ message.displayName }}</span>
        <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="0.95" />
      </div>
      <ChatMessageMeta :message="message" :scale="scale" />
      <div class="leading-snug">
        <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                            :revealed="revealed" @link-click="emit('linkClick', $event)" />
      </div>
    </div>
  </div>
</template>
