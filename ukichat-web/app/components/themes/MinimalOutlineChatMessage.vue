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
const isMention = computed(() => props.message.messageType === 'Mention')

const boxStyle = computed(() => ({
  padding: `${0.15 * s.value}rem 0`,
  fontSize: `${s.value}rem`,
  lineHeight: '1.45',
  textShadow: '0 0 3px #000, 0 0 3px #000, 1px 1px 2px #000',
}))
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" dono-class="text-[#69f0ae] font-bold" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.15" inline />
      <span class="font-extrabold align-middle" :style="{ color: message.displayNameColor }">{{ message.displayName }}:</span>
      <span class="align-middle" :class="isMention ? 'text-[#ff8a80]' : ''"> </span>
      <span class="align-middle" :class="isMention ? 'text-[#ff8a80]' : ''">
        <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                            :revealed="revealed" @link-click="emit('linkClick', $event)" />
      </span>
    </div>
  </div>
</template>
