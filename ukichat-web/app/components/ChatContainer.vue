<script setup lang="ts">
import type {ChatMessage} from "~/types/ChatMessage";
import type {MessageType} from "~/types/ChatMessage";

const props = withDefaults(defineProps<{
  messages: ChatMessage[]
  scale?: number
  hideVerticalScrollbar?: boolean
}>(), {
  scale: 1,
  hideVerticalScrollbar: false
})

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
  if (message.messageType === 'Notification' || message.messageType === 'Mention') {
    base.paddingLeft = `${0.375 * props.scale}rem`
  }
  return base
}

function getMessageClass(messageType: MessageType | undefined) {
  if (messageType === 'Notification') return 'bg-gray-50/10 border-l-[3px] border-gray-50 text-gray-400 rounded-r-sm'
  if (messageType === 'Mention') return 'bg-red-500/10 border-l-[3px] border-red-500 text-red-400 rounded-r-sm'
  return ''
}

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
    <div class="chat-message" v-for="message in messages" :style="getMessageStyle(message)" :class="getMessageClass(message.messageType)">
      <img :style="iconStyle" :alt="message.platform" :src="getPlatformImage(message.platform)">
      <img :style="iconStyle" v-for="badge in message.badges" :key="badge" :src="badge" alt="badge">
      <span class="font-bold align-middle" :style="{marginRight: marginRight, color: message.displayNameColor}">{{ message.displayName }}</span>
      <span class="inline">
        <template v-for="(part, index) in message.messageParts" :key="index">
          <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
          <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline" :style="emoteStyle">
        </template>
      </span>
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
