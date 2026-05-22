<script setup lang="ts">
import type { HubConnection } from '@microsoft/signalr'
import { useSignalR } from '~/composables/useSignalR'
import MenuSettingsItem from '~/components/MenuSettingsItem.vue'
import GeneralSettings from '~/components/settings/GeneralSettings.vue'
import AppearanceSettings from '~/components/settings/AppearanceSettings.vue'
import PlatformSettings from '~/components/settings/PlatformSettings.vue'

const { startSignalR, invokeGet, invokeUpdate } = useSignalR()
const { getLanguage } = useLocalization()
const { t } = useI18n()
const { mainWindowScale, overlayScale } = useScaleSettings()
const { mainWindowTheme, overlayTheme } = useThemeSettings()

const appSettingsInfo = ref({ profileName: '', language: 'en' })
const activeRoot = ref('general')
const currentLanguage = ref('ru')
const connection = ref<HubConnection | null>(null)

const languages = [
  { code: 'ru', flag: '🇷🇺', label: 'Русский' },
  { code: 'en', flag: '🇬🇧', label: 'English' },
]

function selectRoot(key: string) {
  activeRoot.value = key
}

async function changeLanguage(lang: string) {
  if (!connection.value || currentLanguage.value === lang) return
  await getLanguage(lang, connection.value)
  currentLanguage.value = lang
}

const state = reactive({
  settings: {
    twitch: { channel: '' },
    vkVideoLive: { channel: '' }
  }
})

async function changeTwitchChannel(channel: string) {
  await invokeUpdate('ChangeTwitchChannel', channel)
}

async function changeVkVideoLiveChannel(channel: string) {
  await invokeUpdate('ChangeVkVideoLiveChannel', channel)
}

let scaleSettingsLoaded = false
let themeSettingsLoaded = false

watch(mainWindowScale, (val) => {
  if (!scaleSettingsLoaded) return
  invokeUpdate('BroadcastScaleSettings', val, overlayScale.value)
})

watch(overlayScale, (val) => {
  if (!scaleSettingsLoaded) return
  invokeUpdate('BroadcastScaleSettings', mainWindowScale.value, val)
})

watch(mainWindowTheme, (val) => {
  if (!themeSettingsLoaded) return
  invokeUpdate('BroadcastThemeSettings', val, overlayTheme.value)
})

watch(overlayTheme, (val) => {
  if (!themeSettingsLoaded) return
  invokeUpdate('BroadcastThemeSettings', mainWindowTheme.value, val)
})

onMounted(async () => {
  connection.value = await startSignalR()
  appSettingsInfo.value = await invokeGet('GetActiveAppSettingsInfo')
  currentLanguage.value = appSettingsInfo.value.language
  await getLanguage(appSettingsInfo.value.language, connection.value)
  state.settings = await invokeGet('GetActiveAppSettingsData')

  const scaleSettings = await invokeGet('GetScaleSettings')
  mainWindowScale.value = scaleSettings.mainWindowScale
  overlayScale.value = scaleSettings.overlayScale

  const themeSettings = await invokeGet('GetThemeSettings')
  mainWindowTheme.value = themeSettings.mainWindowTheme
  overlayTheme.value = themeSettings.overlayTheme

  await nextTick()
  scaleSettingsLoaded = true
  themeSettingsLoaded = true
})
</script>

<template>
  <div class="flex h-screen bg-gray-950 text-gray-100 overflow-hidden">

    <nav class="w-44 border-r border-gray-800 flex flex-col pt-6 px-2 shrink-0 h-full">
      <div class="flex flex-col gap-0.5">
        <MenuSettingsItem
          :title="t('settings.general.title')"
          :active="activeRoot === 'general'"
          @click="selectRoot('general')"
        >
          <GeneralSettings />
        </MenuSettingsItem>
        <MenuSettingsItem
          :title="t('settings.appearance.title')"
          :active="activeRoot === 'appearance'"
          @click="selectRoot('appearance')"
        >
          <AppearanceSettings />
        </MenuSettingsItem>
        <MenuSettingsItem
          :title="t('settings.platforms.title')"
          :active="activeRoot === 'platforms'"
          @click="selectRoot('platforms')"
        >
          <PlatformSettings
            :twitch-channel="state.settings.twitch.channel"
            :vk-video-live-channel="state.settings.vkVideoLive.channel"
            @save-twitch="changeTwitchChannel"
            @save-vk="changeVkVideoLiveChannel"
          />
        </MenuSettingsItem>
      </div>

      <!-- Выбор языка -->
      <div class="mt-auto pb-4 px-2 flex flex-row justify-center gap-2">
        <button
          v-for="lang in languages"
          :key="lang.code"
          :title="lang.label"
          class="p-1 rounded-md transition-all duration-150"
          :class="currentLanguage === lang.code
            ? 'bg-gray-700 opacity-100'
            : 'opacity-35 hover:opacity-65 hover:bg-gray-800'"
          @click="changeLanguage(lang.code)"
        >
          <img
            :src="`/images/flags/${lang.code}.svg`"
            :alt="lang.label"
            class="w-8 h-5 rounded-sm object-cover"
          />
        </button>
      </div>
    </nav>

    <main id="settings-content" class="flex-1 overflow-y-auto"></main>

  </div>
</template>
