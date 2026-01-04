<script setup lang="ts">
import {onMounted} from 'vue'
import {useSignalR} from "~/composables/useSignalR";

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

async function getActiveAppSettingsInfo() {
  return await invokeGet("GetActiveAppSettingsInfo")
}

async function openSettingsWindow() {
  await invokeUpdate("OpenSettingsWindow")
}

async function connectToTwitch() {
  if (appSettingsInfo.value.twitch.channel !== null) {
    await invokeUpdate("ConnectToTwitch", appSettingsInfo.value.twitch.channel)
  }
}

// Запуск SignalR при монтировании компонента
onMounted(async () => {
  let connection = await startSignalR()
  appSettingsInfo.value = await getActiveAppSettingsInfo()
  await getLanguage(appSettingsInfo.value.language, connection)
  await connectToTwitch()
})

</script>

<template>
  <div class="flex justify-end gap-2 m-2">
    <UButton :title="t('settings.title')" variant="ghost" color="gray" square
             class="hover:bg-gray-800 transition cursor-pointer opacity-25 hover:opacity-100"
             @click="openSettingsWindow"
    >
      <UIcon name="i-mdi-cog" class="w-6 h-6 dark:text-gray-400 transition">

      </UIcon>
    </UButton>
  </div>
</template>