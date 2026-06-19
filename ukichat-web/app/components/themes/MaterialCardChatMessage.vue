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
</script>

<template>
  <div
    class="overflow-hidden bg-white text-[#1f1f1f]"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="{ borderRadius: `${0.5 * s}rem`, marginBottom: `${0.5 * s}rem`, fontSize: `${s}rem`, boxShadow: '0 2px 6px rgba(0,0,0,0.4)' }"
    @click="toggleRevealDeleted"
  >
    <div :style="{ height: `${0.25 * s}rem`, background: getPlatformColor(message.platform) }"></div>
    <div :style="{ padding: `${0.45 * s}rem ${0.7 * s}rem` }">
      <ChatMessageMeta :message="message" :scale="scale" reply-class="text-gray-500" dono-class="text-green-700"
                       reward-class="text-purple-700" bits-class="text-cyan-700" />
      <div class="flex items-center" :style="{ gap, marginBottom: gap }">
        <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.05" />
        <span class="font-bold leading-none" :style="{ color: message.displayNameColor }">{{ message.displayName }}</span>
      </div>
      <div class="leading-snug break-words">
        <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                            :revealed="revealed" @link-click="emit('linkClick', $event)" />
      </div>
    </div>
  </div>
</template>
