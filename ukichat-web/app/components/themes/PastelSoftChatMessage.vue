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

// Пастельная заливка по платформе.
function pastelBg(platform: string): string {
  switch (platform) {
    case "Twitch": return "#ece3ff"
    case "VkVideoLive": return "#e3f0ff"
    case "YouTube": return "#ffe3e3"
    case "DonationAlerts": return "#fff0db"
    default: return "#eceffc"
  }
}

const boxStyle = computed(() => ({
  padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
  marginBottom: `${0.4 * s.value}rem`,
  borderRadius: `${0.9 * s.value}rem`,
  fontSize: `${s.value}rem`,
  color: '#2b2b3a',
  background: pastelBg(props.message.platform),
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-gray-500" dono-class="text-[#1b8a4b] font-bold"
                     reward-class="text-purple-700" bits-class="text-cyan-700" />
    <div class="flex items-center" :style="{ gap, marginBottom: gap }">
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" />
      <span class="font-bold leading-none" :style="{ color: message.displayNameColor }">{{ message.displayName }}</span>
    </div>
    <div class="leading-snug">
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
