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
const avatarSize = computed(() => `${2.2 * s.value}rem`)
const avatarFont = computed(() => `${0.95 * s.value}rem`)
const gap = computed(() => `${0.4 * s.value}rem`)
const headGap = computed(() => `${0.3 * s.value}rem`)
const initial = computed(() => props.message.displayName.charAt(0))

const rowStyle = computed(() => {
  const base: Record<string, string> = {
    padding: `${0.25 * s.value}rem ${0.5 * s.value}rem`,
    borderRadius: `${0.4 * s.value}rem`,
    fontSize: `${s.value}rem`,
  }
  if (messageVariant(props.message.messageType) === 'mention') {
    base.background = 'rgba(250, 166, 26, 0.12)'
    base.borderLeft = `${0.13 * s.value}rem solid #faa61a`
  }
  return base
})
</script>

<template>
  <div
    class="flex items-start"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="[rowStyle, { gap }]"
    @click="toggleRevealDeleted"
  >
    <span class="rounded-full inline-flex items-center justify-center font-extrabold text-white shrink-0"
          :style="{ background: message.displayNameColor, width: avatarSize, height: avatarSize, fontSize: avatarFont }">{{ initial }}</span>
    <div class="min-w-0">
      <div class="flex items-center" :style="{ gap: headGap }">
        <span class="font-semibold" :style="{ color: message.displayNameColor }">{{ message.displayName }}</span>
        <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1" />
      </div>
      <ChatMessageMeta :message="message" :scale="scale" />
      <div class="leading-snug break-words">
        <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                            :revealed="revealed" @link-click="emit('linkClick', $event)" />
      </div>
    </div>
  </div>
</template>
