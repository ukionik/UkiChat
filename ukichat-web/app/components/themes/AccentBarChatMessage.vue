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

const boxStyle = computed(() => ({
  padding: `${0.3 * s.value}rem ${0.65 * s.value}rem`,
  marginBottom: `${0.32 * s.value}rem`,
  fontSize: `${s.value}rem`,
  background: 'rgba(0, 0, 0, 0.4)',
  borderLeft: `${0.25 * s.value}rem solid ${getPlatformColor(props.message.platform)}`,
  borderRadius: `0 ${0.4 * s.value}rem ${0.4 * s.value}rem 0`,
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
      <span class="font-bold align-middle" :style="{ color: message.displayNameColor }">{{ message.displayName }}:</span>
      <span class="align-middle"> </span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
