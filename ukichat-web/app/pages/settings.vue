<script setup lang="ts">
import type { HubConnection } from '@microsoft/signalr'
import type { TwitchAuthStatus } from '~/types/TwitchAuth'
import type { DonationAlertsAuthStatus } from '~/types/DonationAlertsAuth'
import { useSignalR } from '~/composables/useSignalR'
import MenuSettingsItem from '~/components/MenuSettingsItem.vue'
import GeneralSettings from '~/components/settings/GeneralSettings.vue'
import AppearanceSettings from '~/components/settings/AppearanceSettings.vue'
import PlatformSettings from '~/components/settings/PlatformSettings.vue'
import IntegrationsSettings from '~/components/settings/IntegrationsSettings.vue'

const { startSignalR, invokeGet, invokeUpdate } = useSignalR()
const { getLanguage } = useLocalization()
const { t } = useI18n()
const { mainWindowScale, overlayScale } = useScaleSettings()
const { mainWindowTheme, overlayTheme } = useThemeSettings()
const { mainWindowMessageHideDelay, overlayMessageHideDelay } = useMessageHideSettings()
const { overlayHideClippedMessages } = useClipSettings()

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

const twitchAuth = ref<TwitchAuthStatus>({ authorized: false, login: null })
const donationAlertsAuth = ref<DonationAlertsAuthStatus>({ authorized: false, name: null })

async function changeTwitchChannel(channel: string) {
  await invokeUpdate('ChangeTwitchChannel', channel)
}

async function changeVkVideoLiveChannel(channel: string) {
  await invokeUpdate('ChangeVkVideoLiveChannel', channel)
}

async function authorizeTwitch() {
  await invokeUpdate('StartTwitchAuth')
}

async function logoutTwitch() {
  await invokeUpdate('LogoutTwitch')
}

async function authorizeDonationAlerts() {
  await invokeUpdate('StartDonationAlertsAuth')
}

async function logoutDonationAlerts() {
  await invokeUpdate('LogoutDonationAlerts')
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

let messageHideSettingsLoaded = false

watch(mainWindowMessageHideDelay, (val) => {
  if (!messageHideSettingsLoaded) return
  invokeUpdate('BroadcastMessageHideSettings', val, overlayMessageHideDelay.value)
})

watch(overlayMessageHideDelay, (val) => {
  if (!messageHideSettingsLoaded) return
  invokeUpdate('BroadcastMessageHideSettings', mainWindowMessageHideDelay.value, val)
})

let clipSettingsLoaded = false

watch(overlayHideClippedMessages, (val) => {
  if (!clipSettingsLoaded) return
  invokeUpdate('BroadcastClipSettings', val)
})

onMounted(async () => {
  connection.value = await startSignalR()
  appSettingsInfo.value = await invokeGet('GetActiveAppSettingsInfo')
  currentLanguage.value = appSettingsInfo.value.language
  await getLanguage(appSettingsInfo.value.language, connection.value)
  state.settings = await invokeGet('GetActiveAppSettingsData')

  twitchAuth.value = await invokeGet('GetTwitchAuthStatus')
  connection.value?.on('OnTwitchAuthChanged', (status: TwitchAuthStatus) => {
    twitchAuth.value = status
  })

  donationAlertsAuth.value = await invokeGet('GetDonationAlertsAuthStatus')
  connection.value?.on('OnDonationAlertsAuthChanged', (status: DonationAlertsAuthStatus) => {
    donationAlertsAuth.value = status
  })

  const scaleSettings = await invokeGet('GetScaleSettings')
  mainWindowScale.value = scaleSettings.mainWindowScale
  overlayScale.value = scaleSettings.overlayScale

  const themeSettings = await invokeGet('GetThemeSettings')
  mainWindowTheme.value = themeSettings.mainWindowTheme
  overlayTheme.value = themeSettings.overlayTheme

  const messageHideSettings = await invokeGet('GetMessageHideSettings')
  mainWindowMessageHideDelay.value = messageHideSettings.mainWindowMessageHideDelay
  overlayMessageHideDelay.value = messageHideSettings.overlayMessageHideDelay

  const clipSettings = await invokeGet('GetClipSettings')
  overlayHideClippedMessages.value = clipSettings.overlayHideClippedMessages

  await nextTick()
  scaleSettingsLoaded = true
  themeSettingsLoaded = true
  messageHideSettingsLoaded = true
  clipSettingsLoaded = true
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
            :twitch-auth="twitchAuth"
            @save-twitch="changeTwitchChannel"
            @save-vk="changeVkVideoLiveChannel"
            @authorize-twitch="authorizeTwitch"
            @logout-twitch="logoutTwitch"
          />
        </MenuSettingsItem>
        <MenuSettingsItem
          :title="t('settings.integrations.title')"
          :active="activeRoot === 'integrations'"
          @click="selectRoot('integrations')"
        >
          <IntegrationsSettings
            :donation-alerts-auth="donationAlertsAuth"
            @authorize-donation-alerts="authorizeDonationAlerts"
            @logout-donation-alerts="logoutDonationAlerts"
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
