<script setup lang="ts">
import type { ChatMessage } from "~/types/ChatMessage";

const props = withDefaults(defineProps<{
  theme?: ChatTheme
  scale?: number
  // Интервал появления новых сообщений, мс
  interval?: number
  // Сколько сообщений держать в превью
  maxMessages?: number
}>(), {
  theme: 'default',
  scale: 1,
  interval: 1500,
  maxMessages: 30,
})

const messages = ref<ChatMessage[]>([])
let timer: ReturnType<typeof setInterval> | null = null

function pushMessage() {
  messages.value = [...messages.value, createSampleMessage()].slice(-props.maxMessages)
}

onMounted(() => {
  // Стартовый набор, чтобы превью не было пустым
  for (let i = 0; i < 4; i++) pushMessage()
  timer = setInterval(pushMessage, props.interval)
})

onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
  timer = null
})
</script>

<template>
  <div class="theme-preview">
    <ChatContainer
      :messages="messages"
      :theme="theme"
      :scale="scale"
      :fill-height="false"
      :hide-vertical-scrollbar="true"
    />
  </div>
</template>

<style scoped>
.theme-preview {
  height: 100%;
  width: 100%;
  overflow: hidden;
  border-radius: 0.5rem;
  /* Нейтральный тёмный фон под чатом */
  background: #18181b;
}
</style>
