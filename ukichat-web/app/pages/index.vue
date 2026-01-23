<script setup lang="ts">
import {onMounted} from 'vue'
import {useSignalR} from "~/composables/useSignalR";
import type {ChatMessage} from "~/types/ChatMessage";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()
const {getLanguage} = useLocalization()
const {t} = useI18n()

const appSettingsInfo = ref({
  profileName: "",
  language: "en",
  twitch: {
    channel: null
  }
})

const maxMessages = 1000
const chatMessages = ref<ChatMessage[]>([])
const chatContainer = ref<HTMLElement | null>(null)
const autoScroll = ref(true)

function addItem(item: ChatMessage) {
  return [...chatMessages.value, item].slice(-maxMessages)
}

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

async function getActiveAppSettingsInfo() {
  return await invokeGet("GetActiveAppSettingsInfo")
}

async function openSettingsWindow() {
  await invokeUpdate("OpenSettingsWindow")
}

async function connectToTwitch() {
  try{
    await invokeUpdate("ConnectToTwitch")
  } catch (error) {
    console.error(error)
  }
}

watch(chatMessages, async () => {
  await nextTick()
  if (autoScroll.value) {
    scrollToBottom()
  }
})

// Запуск SignalR при монтировании компонента
onMounted(async () => {
  let connection = await startSignalR()
  appSettingsInfo.value = await getActiveAppSettingsInfo()
  await getLanguage(appSettingsInfo.value.language, connection)
  await connectToTwitch()

  connection.on("OnChatMessage", (message: ChatMessage) => {
    chatMessages.value = addItem(message)
    console.log(message)
  })

  connection.on("OnTwitchReconnect", async () => {
    console.log("Reconnecting...")
    await connectToTwitch()
  })
})

</script>

<template>
  <div class="fixed top-2 right-2 z-50">
    <UButton :title="t('settings.title')" variant="ghost" color="gray" square
             class="hover:bg-gray-800 transition cursor-pointer opacity-25 hover:opacity-100"
             @click="openSettingsWindow"
    >
      <UIcon name="i-mdi-cog" class="w-6 h-6 dark:text-gray-400 transition">

      </UIcon>
    </UButton>
  </div>
  <div class="chat-container h-dvh overflow-y-auto" ref="chatContainer" @scroll="onScroll">
    <div class="chat-message flex items-center" v-for="message in chatMessages">
      <img class="w-6 h-6" :alt="message.platform" :src="getPlatformImage(message.platform)">
      <img v-for="badge in message.badges" :key="badge" :src="badge" alt="badge" class="w-4 h-4 ml-1">
      <span class="font-bold ml-1">{{ message.displayName }}</span>
      <span class="ml-1">
        <template v-for="(part, index) in message.messageParts" :key="index">
          <span v-if="part.type === 'Text'">{{ part.content }}</span>
          <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline-block w-7 h-7 align-middle">
        </template>
      </span>
    </div>
  </div>
</template>