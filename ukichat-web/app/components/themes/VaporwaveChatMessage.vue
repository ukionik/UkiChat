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
  padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
  marginBottom: `${0.45 * s.value}rem`,
  borderRadius: `${0.4 * s.value}rem`,
  fontSize: `${s.value}rem`,
  background: 'linear-gradient(135deg, rgba(255,90,200,0.35), rgba(120,90,255,0.35))',
  border: '1px solid rgba(255, 150, 230, 0.5)',
  boxShadow: '0 0 10px rgba(255, 90, 200, 0.3)',
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" dono-class="text-[#aaffea] font-bold" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.15" inline />
      <span class="font-extrabold align-middle" :style="{ color: message.displayNameColor, textShadow: '0 0 6px rgba(255,255,255,0.6)', letterSpacing: '0.5px' }">{{ message.displayName }}:</span>
      <span class="align-middle"> </span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
