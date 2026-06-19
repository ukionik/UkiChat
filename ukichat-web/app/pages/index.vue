<script setup lang="ts">
import {onMounted} from 'vue'
import {useSignalR} from "~/composables/useSignalR";
import type {ChatMessage} from "~/types/ChatMessage";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()
const {getLanguage} = useLocalization()
const { mainWindowScale, mainWindowScaleFactor } = useScaleSettings()
const { mainWindowTheme } = useThemeSettings()
const { mainWindowMessageHideDelay } = useMessageHideSettings()

// Переопределение темы через ?theme=<тема> для тестирования.
// Если параметр не указан или тема неизвестна — используется тема из настроек.
const route = useRoute()
const themeOverride = computed(() => {
  const q = Array.isArray(route.query.theme) ? route.query.theme[0] : route.query.theme
  return isValidTheme(q) ? q : null
})
const effectiveTheme = computed(() => themeOverride.value ?? mainWindowTheme.value)


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
  console.log('[index.vue] onMounted: BEGIN')
  const tMount = performance.now()
  try {
    let connection = await startSignalR()
    console.log(`[index.vue] startSignalR done (+${(performance.now() - tMount).toFixed(0)}ms)`)

    appSettingsInfo.value = await getActiveAppSettingsInfo()
    console.log(`[index.vue] getActiveAppSettingsInfo done lang=${appSettingsInfo.value.language} (+${(performance.now() - tMount).toFixed(0)}ms)`)

    await getLanguage(appSettingsInfo.value.language, connection)
    console.log(`[index.vue] getLanguage done (+${(performance.now() - tMount).toFixed(0)}ms)`)

    const scaleSettings = await invokeGet('GetScaleSettings')
    mainWindowScale.value = scaleSettings.mainWindowScale

    const themeSettings = await invokeGet('GetThemeSettings')
    mainWindowTheme.value = themeSettings.mainWindowTheme

    const messageHideSettings = await invokeGet('GetMessageHideSettings')
    mainWindowMessageHideDelay.value = messageHideSettings.mainWindowMessageHideDelay
    console.log(`[index.vue] all settings loaded (+${(performance.now() - tMount).toFixed(0)}ms)`)

  connection.on("OnScaleSettingsChanged", (main: number, _overlay: number) => {
    mainWindowScale.value = main
  })

  connection.on("OnThemeSettingsChanged", (main: string, _overlay: string) => {
    mainWindowTheme.value = main
  })

  connection.on("OnMessageHideSettingsChanged", (main: number, _overlay: number) => {
    mainWindowMessageHideDelay.value = main
  })

  connection.on("OnChatMessage", (message: ChatMessage) => {
    chatMessages.value = addItem(message)
    if (mainWindowMessageHideDelay.value > 0 && message.id) {
      setTimeout(() => {
        chatMessages.value = chatMessages.value.filter(m => m.id !== message.id)
      }, mainWindowMessageHideDelay.value * 1000)
    }
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

    console.log(`[index.vue] onMounted: END (+${(performance.now() - tMount).toFixed(0)}ms)`)
  } catch (err) {
    console.error(`[index.vue] onMounted FAILED (+${(performance.now() - tMount).toFixed(0)}ms)`, err)
  }
})

function openLink(url: string) {
  invokeUpdate('OpenUrl', url)
}

</script>

<template>
  <ChatContainer :messages="chatMessages" :scale="mainWindowScaleFactor" :theme="effectiveTheme" :allow-reveal-deleted="true" @link-click="openLink" />
</template>