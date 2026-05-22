<script setup lang="ts">
import {onMounted} from 'vue'
import {useSignalR} from "~/composables/useSignalR";
import type {ChatMessage} from "~/types/ChatMessage";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()
const {getLanguage} = useLocalization()
const { mainWindowScale, mainWindowScaleFactor } = useScaleSettings()
const { mainWindowTheme } = useThemeSettings()

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

  const scaleSettings = await invokeGet('GetScaleSettings')
  mainWindowScale.value = scaleSettings.mainWindowScale

  connection.on("OnScaleSettingsChanged", (main: number, _overlay: number) => {
    mainWindowScale.value = main
  })

  connection.on("OnChatMessage", (message: ChatMessage) => {
    chatMessages.value = addItem(message)
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

function openLink(url: string) {
  invokeUpdate('OpenUrl', url)
}

</script>

<template>
  <ChatContainer :messages="chatMessages" :scale="mainWindowScaleFactor" :theme="mainWindowTheme" :allow-reveal-deleted="true" @link-click="openLink" />
</template>