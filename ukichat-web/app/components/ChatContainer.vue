<script setup lang="ts">
import type {ChatMessage} from "~/types/ChatMessage";

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

const emit = defineEmits<{
  linkClick: [url: string]
}>()

const chatContainer = ref<HTMLElement | null>(null)
const autoScroll = ref(true)

function scrollToBottom() {
  if (!chatContainer.value) return
  chatContainer.value.scrollTop = chatContainer.value.scrollHeight
}

function onScroll() {
  const el = chatContainer.value
  if (!el) return
  const threshold = 200 * props.scale
  autoScroll.value = el.scrollTop + el.clientHeight >= el.scrollHeight - threshold
}

const chatStyle = computed(() => ({
  padding: `${0.25 * props.scale}rem ${0.5 * props.scale}rem`,
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
    <ThemesBoxChatMessage
      v-for="message in messages"
      :key="message.id"
      :message="message"
      :scale="scale"
      :allowRevealDeleted="allowRevealDeleted"
      @linkClick="emit('linkClick', $event)"
    />
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
