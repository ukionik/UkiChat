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

// Переопределение темы через ?theme=<тема> для тестирования.
// Если параметр не указан или тема неизвестна — используется тема из настроек.
const route = useRoute()
const themeOverride = computed(() => {
  const q = Array.isArray(route.query.theme) ? route.query.theme[0] : route.query.theme
  return isValidTheme(q) ? q : null
})
const effectiveTheme = computed(() => themeOverride.value ?? overlayTheme.value)

// Переопределение фона страницы через ?bg=<цвет>.
// Принимается любой валидный CSS-цвет: transparent, red, %23ff0000 (или просто ff0000),
// rgba(0,0,0,.5) и т.д. Если параметр не указан или значение невалидно —
// остаётся фон по умолчанию из main.css (#000).
function normalizeBackground(value: string | null | undefined): string | null {
  if (!value) return null
  let color = value.trim()
  if (!color) return null
  // Голый hex без решётки: в URL «#» приходится экранировать, поэтому разрешаем и без него.
  if (/^[0-9a-f]{3,8}$/i.test(color) && [3, 4, 6, 8].includes(color.length)) {
    color = `#${color}`
  }
  // Проверка браузером отсекает мусор и попытки подставить произвольный CSS.
  if (typeof CSS === 'undefined' || !CSS.supports('background-color', color)) return null
  return color
}

const backgroundOverride = computed(() => {
  const q = Array.isArray(route.query.bg) ? route.query.bg[0] : route.query.bg
  return normalizeBackground(q)
})

useHead({
  bodyAttrs: {
    style: computed(() => backgroundOverride.value ? `background: ${backgroundOverride.value};` : ''),
  },
})

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
    overlayTheme.value = overlay
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
  <ChatContainer :messages="chatMessages" :scale="overlayScaleFactor" :theme="effectiveTheme" :hide-vertical-scrollbar="true" :hide-clipped="overlayHideClippedMessages" />
</template>
