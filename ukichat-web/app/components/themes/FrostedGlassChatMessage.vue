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
const gap = computed(() => `${0.35 * s.value}rem`)
const nameFontSize = computed(() => `${0.95 * s.value}rem`)

const boxStyle = computed(() => ({
  padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
  marginBottom: `${0.4 * s.value}rem`,
  borderRadius: `${0.6 * s.value}rem`,
  fontSize: `${s.value}rem`,
  background: 'rgba(255, 255, 255, 0.18)',
  border: '1px solid rgba(255, 255, 255, 0.35)',
  backdropFilter: 'blur(6px)',
  boxShadow: '0 2px 8px rgba(0, 0, 0, 0.2)',
}))
</script>

<template>
  <div
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" with-margin reply-class="text-gray-200" />
    <div class="flex items-center" :style="{ gap, marginBottom: gap }">
      <ChatPlatformBadges :message="message" :scale="scale" />
      <span class="font-bold leading-none"
            :style="{ color: message.displayNameColor, fontSize: nameFontSize }">{{ message.displayName }}</span>
    </div>
    <div class="leading-snug break-words">
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
