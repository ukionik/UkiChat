<script setup lang="ts">
import {onMounted} from 'vue'
import {useSignalR} from "~/composables/useSignalR";
import type {ChatMessage} from "~/types/ChatMessage";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()
const {getLanguage} = useLocalization()

const appSettingsInfo = ref({
  profileName: "",
  language: "en",
  twitch: {
    channel: null
  },
  vkVideoLive:{
    channel: null
  }
})

const maxMessages = 1000
const chatMessages = ref<ChatMessage[]>([])

function addItem(item: ChatMessage) {
  return [...chatMessages.value, item].slice(-maxMessages)
}

async function getActiveAppSettingsInfo() {
  return await invokeGet("GetActiveAppSettingsInfo")
}

// Запуск SignalR при монтировании компонента
onMounted(async () => {
  let connection = await startSignalR()
  appSettingsInfo.value = await getActiveAppSettingsInfo()
  await getLanguage(appSettingsInfo.value.language, connection)

  connection.on("OnChatMessage", (message: ChatMessage) => {
    chatMessages.value = addItem(message)
  })
})

function openLink(url: string) {
  invokeUpdate('OpenUrl', url)
}

</script>

<template>
  <ChatContainer :messages="chatMessages" :scale="2.0" @link-click="openLink" />
</template>