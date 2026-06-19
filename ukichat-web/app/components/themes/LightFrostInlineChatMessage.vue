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
  padding: `${0.2 * s.value}rem ${0.6 * s.value}rem`,
  fontSize: `${s.value}rem`,
  lineHeight: '1.4',
  color: '#1b1b24',
  background: 'rgba(228, 228, 238, 0.82)',
  backdropFilter: 'blur(2px)',
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-gray-600" dono-class="text-green-700"
                     reward-class="text-purple-700" bits-class="text-cyan-700" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.1" inline />
      <span class="font-extrabold align-middle" :style="{ color: message.displayNameColor }">{{ message.displayName }}:</span>
      <span class="align-middle"> </span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
