<script setup lang="ts">
import {onMounted} from 'vue'
import {useSignalR} from "~/composables/useSignalR";
import type {ChatMessage} from "~/types/ChatMessage";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()
const {getLanguage} = useLocalization()
const { overlayScale, overlayScaleFactor } = useScaleSettings()

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

onMounted(async () => {
  let connection = await startSignalR()
  appSettingsInfo.value = await getActiveAppSettingsInfo()
  await getLanguage(appSettingsInfo.value.language, connection)

  connection.on("OnChatMessage", (message: ChatMessage) => {
    chatMessages.value = addItem(message)
  })

  connection.on("OnScaleSettingsChanged", (_main: number, overlay: number) => {
    overlayScale.value = overlay
  })

  connection.on("OnMessageDeleted", (messageId: string) => {
    const msg = chatMessages.value.find(m => m.id === messageId)
    if (msg) msg.messageType = 'Deleted'
  })

  connection.on("OnUserMessagesDeleted", (username: string) => {
    chatMessages.value
      .filter(m => m.displayName.toLowerCase() === username.toLowerCase() && m.messageType !== 'Deleted')
      .forEach(m => m.messageType = 'Deleted')
  })
})
</script>

<template>
  <ChatContainer :messages="chatMessages" :scale="overlayScaleFactor" :hide-vertical-scrollbar="true" />
</template>
