<script setup lang="ts">
import {onMounted} from 'vue'
import {useSignalR} from "~/composables/useSignalR";
import type {ChatMessage} from "~/types/ChatMessage";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()
const {getLanguage} = useLocalization()
const { overlayScale, overlayScaleFactor } = useScaleSettings()
const { overlayTheme } = useThemeSettings()
const { overlayMessageHideDelay } = useMessageHideSettings()
const { overlayHideClippedMessages } = useClipSettings()

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
  console.log('[chat.vue] onMounted: BEGIN')
  const tMount = performance.now()
  try {
    let connection = await startSignalR()
    console.log(`[chat.vue] startSignalR done (+${(performance.now() - tMount).toFixed(0)}ms)`)

    appSettingsInfo.value = await getActiveAppSettingsInfo()
    console.log(`[chat.vue] getActiveAppSettingsInfo done (+${(performance.now() - tMount).toFixed(0)}ms)`)

    await getLanguage(appSettingsInfo.value.language, connection)
    console.log(`[chat.vue] getLanguage done (+${(performance.now() - tMount).toFixed(0)}ms)`)

    const scaleSettings = await invokeGet('GetScaleSettings')
    overlayScale.value = scaleSettings.overlayScale

    const themeSettings = await invokeGet('GetThemeSettings')
    overlayTheme.value = themeSettings.overlayTheme

    const messageHideSettings = await invokeGet('GetMessageHideSettings')
    overlayMessageHideDelay.value = messageHideSettings.overlayMessageHideDelay

    const clipSettings = await invokeGet('GetClipSettings')
    overlayHideClippedMessages.value = clipSettings.overlayHideClippedMessages
    console.log(`[chat.vue] all settings loaded (+${(performance.now() - tMount).toFixed(0)}ms)`)

  connection.on("OnThemeSettingsChanged", (_main: string, overlay: string) => {
    overlayTheme.value = overlay as 'default' | 'box'
  })

  connection.on("OnMessageHideSettingsChanged", (_main: number, overlay: number) => {
    overlayMessageHideDelay.value = overlay
  })

  connection.on("OnChatMessage", (message: ChatMessage) => {
    chatMessages.value = addItem(message)
    if (overlayMessageHideDelay.value > 0 && message.id) {
      setTimeout(() => {
        chatMessages.value = chatMessages.value.filter(m => m.id !== message.id)
      }, overlayMessageHideDelay.value * 1000)
    }
  })

  connection.on("OnScaleSettingsChanged", (_main: number, overlay: number) => {
    overlayScale.value = overlay
  })

  connection.on("OnClipSettingsChanged", (overlay: boolean) => {
    overlayHideClippedMessages.value = overlay
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

    console.log(`[chat.vue] onMounted: END (+${(performance.now() - tMount).toFixed(0)}ms)`)
  } catch (err) {
    console.error(`[chat.vue] onMounted FAILED (+${(performance.now() - tMount).toFixed(0)}ms)`, err)
  }
})
</script>

<template>
  <ChatContainer :messages="chatMessages" :scale="overlayScaleFactor" :theme="overlayTheme" :hide-vertical-scrollbar="true" :hide-clipped="overlayHideClippedMessages" />
</template>
