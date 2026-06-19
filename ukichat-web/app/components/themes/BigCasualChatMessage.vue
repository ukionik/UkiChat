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
const nameFontSize = computed(() => `${0.85 * s.value}rem`)

const boxStyle = computed(() => ({
  padding: `${0.25 * s.value}rem ${0.12 * s.value}rem`,
  marginBottom: `${0.4 * s.value}rem`,
  fontSize: `${1.1 * s.value}rem`,
  lineHeight: '1.35',
  textShadow: '0 1px 3px rgba(0, 0, 0, 0.7)',
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <div class="flex items-center" :style="{ gap }">
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1" />
      <span class="font-extrabold leading-none"
            :style="{ color: message.displayNameColor, fontSize: nameFontSize }">{{ message.displayName }}</span>
    </div>
    <ChatMessageMeta :message="message" :scale="scale" dono-class="text-[#69f0ae] font-bold" />
    <div class="leading-snug">
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
