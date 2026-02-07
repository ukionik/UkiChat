<script setup lang="ts">
import { onMounted } from 'vue'
import * as v from 'valibot'
import HorizontalFormField from "~/components/HorizontalFormField.vue";
import {useSignalR} from "~/composables/useSignalR";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()
const { getLanguage } = useLocalization()
const { t } = useI18n()

const appSettingsInfo = ref({
  profileName: "",
  language: "en"
})

const schema = v.object({
  settings: v.object({
    twitch: v.object({
      channel: v.pipe(v.string(), v.minLength(1, "Test"))
    }),
    vkVideoLive: v.object({
      channel: v.pipe(v.string(), v.minLength(1, "Test"))
    }),
  })
})

const state = reactive({
  settings:{
    twitch: {
      channel: '',
    },
    vkVideoLive: {
      channel: '',
    }
  }
})

async function getActiveAppSettingsInfo() {
  return await invokeGet("GetActiveAppSettingsInfo")
}

async function getActiveAppSettingsData(){
  return await invokeGet("GetActiveAppSettingsData")
}

async function changeTwitchChannel() {
  await invokeUpdate("ChangeTwitchChannel", state.settings.twitch.channel)
}

async function changeVkVideoLiveChannel() {
  await invokeUpdate("ChangeVkVideoLiveChannel", state.settings.vkVideoLive.channel)
}


// Запуск SignalR при монтировании компонента
onMounted(async () => {
  let connection = await startSignalR()
  appSettingsInfo.value = await getActiveAppSettingsInfo()
  await getLanguage(appSettingsInfo.value.language, connection)
  state.settings = await getActiveAppSettingsData()
})
</script>

<template>
  <UForm :schema="schema" :state="state" class="space-y-4 m-4">
    <h2 class="text-xl font-semibold mb-4">{{t('settings.twitch.name')}}</h2>
    <HorizontalFormField :label="t('settings.channel')" name="twitch-channel">
      <UInput v-model="state.settings.twitch.channel" @blur="changeTwitchChannel" />
    </HorizontalFormField>
    <h2 class="text-xl font-semibold mb-4">{{t('settings.vkVideoLive.name')}}</h2>
    <HorizontalFormField :label="t('settings.channel')" name="vk-video-live-channel">
      <UInput v-model="state.settings.vkVideoLive.channel" @blur="changeVkVideoLiveChannel" />
    </HorizontalFormField>
  </UForm>
</template>

<style scoped>

</style>