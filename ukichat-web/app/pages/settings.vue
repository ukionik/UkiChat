<script setup lang="ts">
import { useSignalR } from '~/composables/useSignalR'
import MenuSettingsItem from '~/components/MenuSettingsItem.vue'
import GeneralSettings from '~/components/settings/GeneralSettings.vue'
import AppearanceSettings from '~/components/settings/AppearanceSettings.vue'
import PlatformSettings from '~/components/settings/PlatformSettings.vue'

const { startSignalR, invokeGet, invokeUpdate } = useSignalR()
const { getLanguage } = useLocalization()
const { t } = useI18n()

const appSettingsInfo = ref({ profileName: '', language: 'en' })
const activeRoot = ref('general')

function selectRoot(key: string) {
  activeRoot.value = key
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

onMounted(async () => {
  const connection = await startSignalR()
  appSettingsInfo.value = await invokeGet('GetActiveAppSettingsInfo')
  await getLanguage(appSettingsInfo.value.language, connection)
  state.settings = await invokeGet('GetActiveAppSettingsData')
})
</script>

<template>
  <div class="flex h-screen bg-gray-950 text-gray-100 overflow-hidden">

    <nav class="w-44 border-r border-gray-800 flex flex-col pt-6 px-2 gap-0.5 shrink-0">
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
    </nav>

    <main id="settings-content" class="flex-1 overflow-y-auto"></main>

  </div>
</template>
