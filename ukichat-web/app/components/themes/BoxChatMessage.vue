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
const nameFontSize = computed(() => `${0.9 * s.value}rem`)
const gap = computed(() => `${0.35 * s.value}rem`)

const boxStyle = computed(() => {
  const base = {
    padding: `${0.5 * s.value}rem ${0.65 * s.value}rem`,
    borderRadius: `${0.35 * s.value}rem`,
    marginBottom: `${0.4 * s.value}rem`,
    fontSize: `${s.value}rem`,
    boxShadow: '0 0 4px rgba(0, 0, 0, 0.4)',
  }

  const type = props.message.messageType
  if (type === 'Mention') {
    return { ...base, background: 'linear-gradient(rgba(120, 40, 40, 0.7), rgba(90, 20, 20, 0.8))' }
  }
  if (type === 'Notification') {
    return { ...base, background: 'linear-gradient(rgba(60, 60, 60, 0.6), rgba(40, 40, 40, 0.7))' }
  }
  if (type === 'ChannelPointsRedemption') {
    return { ...base, background: 'linear-gradient(rgba(100, 40, 140, 0.6), rgba(70, 20, 110, 0.7))' }
  }
  if (type === 'Donation') {
    return { ...base, background: 'linear-gradient(rgba(34, 120, 70, 0.6), rgba(20, 90, 50, 0.7))' }
  }
  if (type === 'Subscription') {
    return { ...base, background: 'linear-gradient(rgba(110, 70, 180, 0.6), rgba(80, 45, 140, 0.7))' }
  }
  if (type === 'Raid') {
    return { ...base, background: 'linear-gradient(rgba(40, 90, 170, 0.6), rgba(25, 65, 130, 0.7))' }
  }
  if (type === 'Cheer') {
    return { ...base, background: 'linear-gradient(rgba(30, 130, 150, 0.6), rgba(20, 100, 120, 0.7))' }
  }
  return { ...base, background: 'linear-gradient(rgba(70, 70, 70, 0.6), rgba(50, 50, 50, 0.7))' }
})
</script>

<template>
  <div
    class="chat-message"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" with-margin />

    <!-- Шапка: иконка платформы + бейджи + имя -->
    <div class="flex items-center" :style="{ gap, marginBottom: gap }">
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.1" />
      <span class="font-bold leading-none"
            :style="{ color: message.displayNameColor, fontSize: nameFontSize }">{{ message.displayName }}</span>
    </div>

    <!-- Текст сообщения -->
    <div class="leading-snug break-words">
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>

<style scoped>
.chat-message {
  animation: slideIn 250ms ease;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(-12px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}
</style>
