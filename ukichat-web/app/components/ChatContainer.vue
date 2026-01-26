<script setup lang="ts">
import type {ChatMessage} from "~/types/ChatMessage";

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

  const threshold = 200
  autoScroll.value = el.scrollTop + el.clientHeight >= el.scrollHeight - threshold
}

function getPlatformImage(platform: string) {
  switch (platform) {
    case "Twitch": return "/images/twitch.svg"
    default: return ""
  }
}

const messageStyle = computed(() => ({
  fontSize: `${props.scale}rem`
}))

const platformIconSize = computed(() => `${1.25 * props.scale}rem`)
const badgeIconSize = computed(() => `${1.25 * props.scale}rem`)
const emoteIconSize = computed(() => `${1.75 * props.scale}rem`)

const containerClass = computed(() => ({
  'hide-scrollbar': props.hideVerticalScrollbar
}))

watch(() => props.messages, async () => {
  await nextTick()
  if (autoScroll.value) {
    scrollToBottom()
  }
})
</script>

<template>
  <div class="chat-container h-dvh overflow-y-auto" :class="containerClass" ref="chatContainer" @scroll="onScroll">
    <div class="chat-message py-0.5" v-for="message in messages" :style="messageStyle">
      <img class="inline" :style="{ height: platformIconSize }" :alt="message.platform" :src="getPlatformImage(message.platform)">
      <img v-for="badge in message.badges" :key="badge" :src="badge" alt="badge" class="inline ml-1" :style="{ height: badgeIconSize }">
      <span class="font-bold ml-1 align-middle">{{ message.displayName }}</span>
      <span class="ml-1 inline">
        <template v-for="(part, index) in message.messageParts" :key="index">
          <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
          <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline" :style="{ height: emoteIconSize }">
        </template>
      </span>
    </div>
  </div>
</template>

<style scoped>
.hide-scrollbar {
  scrollbar-width: none; /* Firefox */
  -ms-overflow-style: none; /* IE and Edge */
}

.hide-scrollbar::-webkit-scrollbar {
  display: none; /* Chrome, Safari, Opera */
}
</style>
