<script setup lang="ts">
import type {ChatMessage, MessageType, ReplyInfo} from "~/types/ChatMessage";
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

const props = withDefaults(defineProps<{
  messages: ChatMessage[]
  scale?: number
  hideVerticalScrollbar?: boolean
  allowRevealDeleted?: boolean
}>(), {
  scale: 1,
  hideVerticalScrollbar: false,
  allowRevealDeleted: false
})

const revealedMessages = ref(new Set<string>())

function toggleRevealDeleted(message: ChatMessage) {
  if (!props.allowRevealDeleted || message.messageType !== 'Deleted' || !message.id) return
  const next = new Set(revealedMessages.value)
  if (next.has(message.id)) {
    next.delete(message.id)
  } else {
    next.add(message.id)
  }
  revealedMessages.value = next
}

const emit = defineEmits<{
  linkClick: [url: string]
}>()

function handleLinkClick(url: string) {
  emit('linkClick', url)
}

const chatContainer = ref<HTMLElement | null>(null)
const autoScroll = ref(true)

function scrollToBottom() {
  if (!chatContainer.value) {
    return
  }

  chatContainer.value.scrollTop = chatContainer.value.scrollHeight
}

function onScroll() {
  const el = chatContainer.value
  if (!el)
    return

  const threshold = 200 * props.scale
  autoScroll.value = el.scrollTop + el.clientHeight >= el.scrollHeight - threshold
}

function getPlatformImage(platform: string) {
  switch (platform) {
    case "Twitch":
      return "/images/twitch.svg"
    case "VkVideoLive":
      return "/images/vk-video-live.svg"
    default:
      return ""
  }
}

function getMessageStyle(message: ChatMessage) {
  const base: Record<string, string> = { fontSize: `${props.scale}rem` }
  if (message.messageType === 'Notification' || message.messageType === 'Mention' || message.messageType === 'Reply') {
    base.paddingLeft = `${0.375 * props.scale}rem`
  }
  return base
}

function getMessageClass(messageType: MessageType | undefined) {
  if (messageType === 'Notification') return 'bg-gray-50/10 border-l-[3px] border-gray-50 text-gray-400 rounded-r-sm'
  if (messageType === 'Mention') return 'bg-red-500/10 border-l-[3px] border-red-500 text-red-400 rounded-r-sm'
  if (messageType === 'Reply') return 'border-l-[3px] border-gray-500 rounded-r-sm'
  if (messageType === 'Deleted') return ''
  return ''
}

function getReplyPreview(replyTo: ReplyInfo) {
  return replyTo.messageParts.filter(p => p.type === 'Text').map(p => p.content).join('').trim()
}

const replyHeaderStyle = computed(() => ({
  fontSize: `${0.8 * props.scale}rem`,
  marginBottom: `${0.1 * props.scale}rem`,
}))

const chatStyle = computed(() => ({
  padding: `${0.25 * props.scale}rem ${0.5 * props.scale}rem`,
}))

const marginRight = computed(() => `${0.25 * props.scale}rem`)
const iconStyle = computed(() => ({
  display: "inline",
  height: `${1.25 * props.scale}rem`,
  marginRight: marginRight.value,
}))

const emoteStyle = computed(() => ({
  display: "inline",
  height: `${1.5 * props.scale}rem`
}))

const containerClass = computed(() => {
  return props.hideVerticalScrollbar ? 'overflow-y-hidden' : 'overflow-y-auto'
})

watch(() => props.messages, async () => {
  await nextTick()
  if (autoScroll.value) {
    scrollToBottom()
  }
})
</script>

<template>
  <div class="chat-container h-dvh overflow-x-hidden" :style="chatStyle" :class="containerClass" ref="chatContainer"
       @scroll="onScroll">
    <div class="chat-message" v-for="message in messages" :style="getMessageStyle(message)"
         :class="[getMessageClass(message.messageType), message.messageType === 'Deleted' ? (allowRevealDeleted ? '' : 'opacity-50') : '']"
         @click="toggleRevealDeleted(message)">
      <div v-if="message.replyTo" class="flex items-center gap-1 text-gray-400 truncate" :style="replyHeaderStyle">
        <span>↩</span>
        <span class="font-semibold shrink-0">@{{ message.replyTo.displayName }}:</span>
        <span class="truncate opacity-75">{{ getReplyPreview(message.replyTo) }}</span>
      </div>
      <div>
        <img :style="iconStyle" :alt="message.platform" :src="getPlatformImage(message.platform)">
        <img :style="iconStyle" v-for="badge in message.badges" :key="badge" :src="badge" alt="badge">
        <span v-if="message.messageType !== 'Notification'" class="font-bold align-middle"
              :style="{marginRight: marginRight, color: message.displayNameColor}">{{ message.displayName }}:</span>
        <span class="inline">
          <template v-if="message.messageType === 'Deleted'">
            <template v-if="allowRevealDeleted && message.id && revealedMessages.has(message.id)">
              <template v-for="(part, index) in message.messageParts" :key="index">
                <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
                <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline" :style="emoteStyle">
                <span v-else-if="part.type === 'Link'" class="inline align-middle text-blue-400 cursor-pointer hover:underline" @click.stop="handleLinkClick(part.content)">{{ part.content }}</span>
              </template>
            </template>
            <span v-else class="inline align-middle italic"
                  :class="allowRevealDeleted ? 'text-purple-400 cursor-pointer hover:underline' : 'text-gray-300 opacity-70'">{{ t('chat.messageDeleted') }}</span>
          </template>
          <template v-else v-for="(part, index) in message.messageParts" :key="index">
            <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
            <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline" :style="emoteStyle">
            <span v-else-if="part.type === 'Link'" class="inline align-middle text-blue-400 cursor-pointer hover:underline" @click="handleLinkClick(part.content)">{{ part.content }}</span>
          </template>
        </span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.hide-scrollbar {
  scrollbar-width: none;
  -ms-overflow-style: none;
}

.hide-scrollbar::-webkit-scrollbar {
  display: none;
}


</style>
