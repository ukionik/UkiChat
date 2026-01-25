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

async function connectToTwitch() {
  try {
    await invokeUpdate("ConnectToTwitch")
  } catch (error) {
    console.error(error)
  }
}

onMounted(async () => {
  let connection = await startSignalR()
  appSettingsInfo.value = await getActiveAppSettingsInfo()
  await getLanguage(appSettingsInfo.value.language, connection)
  await connectToTwitch()

  connection.on("OnChatMessage", (message: ChatMessage) => {
    chatMessages.value = addItem(message)
  })

  connection.on("OnTwitchReconnect", async () => {
    await connectToTwitch()
  })
})
</script>

<template>
  <ChatContainer :messages="chatMessages" />
</template>
