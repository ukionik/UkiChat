<script setup lang="ts">
import type { ChatMessage, MessageType } from "~/types/ChatMessage";

const props = defineProps<{
  message: ChatMessage
  scale: number
  allowRevealDeleted: boolean
}>()

const emit = defineEmits<{
  linkClick: [url: string]
}>()

const { revealed, toggleRevealDeleted } = useThemeMessage(props, emit)

function getMessageStyle() {
  const base: Record<string, string> = { fontSize: `${props.scale}rem` }
  const type = props.message.messageType
  if (type === 'Notification' || type === 'Mention' || type === 'Reply' || type === 'ChannelPointsRedemption'
      || type === 'Donation' || type === 'Subscription' || type === 'Raid' || type === 'Cheer') {
    base.paddingLeft = `${0.375 * props.scale}rem`
  }
  return base
}

function getMessageClass(messageType: MessageType | undefined) {
  if (messageType === 'Notification') return 'bg-gray-50/10 border-l-[3px] border-gray-50 text-gray-400 rounded-r-sm'
  if (messageType === 'Mention') return 'bg-red-500/10 border-l-[3px] border-red-500 text-red-400 rounded-r-sm'
  if (messageType === 'Reply') return 'border-l-[3px] border-gray-500 rounded-r-sm'
  if (messageType === 'ChannelPointsRedemption') return 'bg-purple-500/10 border-l-[3px] border-purple-400 rounded-r-sm'
  if (messageType === 'Donation') return 'bg-green-500/10 border-l-[3px] border-green-400 rounded-r-sm'
  if (messageType === 'Subscription') return 'bg-violet-500/10 border-l-[3px] border-violet-400 rounded-r-sm'
  if (messageType === 'Raid') return 'bg-blue-500/10 border-l-[3px] border-blue-400 rounded-r-sm'
  if (messageType === 'Cheer') return 'bg-cyan-500/10 border-l-[3px] border-cyan-400 rounded-r-sm'
  return ''
}

const marginRight = computed(() => `${0.25 * props.scale}rem`)
</script>

<template>
  <div class="chat-message break-words"
       :style="getMessageStyle()"
       :class="[getMessageClass(message.messageType), message.messageType === 'Deleted' ? (allowRevealDeleted ? '' : 'opacity-50') : '']"
       @click="toggleRevealDeleted">
    <ChatMessageMeta :message="message" :scale="scale" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.25" inline />
      <span class="font-bold align-middle"
            :style="{ marginRight, color: message.displayNameColor }">{{ message.displayName }}:</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
