<script setup lang="ts">
import type {ChatMessage} from "~/types/ChatMessage";

const props = defineProps<{
  messages: ChatMessage[]
}>()

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

watch(() => props.messages, async () => {
  await nextTick()
  if (autoScroll.value) {
    scrollToBottom()
  }
})
</script>

<template>
  <div class="chat-container h-dvh overflow-y-auto" ref="chatContainer" @scroll="onScroll">
    <div class="chat-message py-0.5" v-for="message in messages">
      <img class="inline h-5" :alt="message.platform" :src="getPlatformImage(message.platform)">
      <img v-for="badge in message.badges" :key="badge" :src="badge" alt="badge" class="inline h-5 ml-1">
      <span class="font-bold ml-1 align-middle">{{ message.displayName }}</span>
      <span class="ml-1 inline">
        <template v-for="(part, index) in message.messageParts" :key="index">
          <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
          <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline h-7">
        </template>
      </span>
    </div>
  </div>
</template>
