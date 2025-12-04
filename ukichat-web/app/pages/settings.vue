<script setup lang="ts">
import { onMounted } from 'vue'
import { useLocalization } from '~/composables/useLocalization'
import * as v from 'valibot'
import HorizontalFormField from "~/components/HorizontalFormField.vue";
import {useSignalR} from "~/composables/useSignalR";

const {startSignalR, invokeUpdate} = useSignalR()
const { t } = useI18n()

const schema = v.object({
  settings: v.object({
    twitch: v.object({
      channel: v.pipe(v.string(), v.minLength(1, "Test"))
    })
  })
})

const state = reactive({
  settings:{
    twitch: {
      channel: '',
    }
  }
})

async function updateSettings() {
  console.log(state.settings.twitch)
  await invokeUpdate("UpdateTwitchSettings", state.settings.twitch)
}

// Запуск SignalR при монтировании компонента
onMounted(() => {
  startSignalR()
})
</script>

<template>
  <UForm :schema="schema" :state="state" class="space-y-4">
    <h2 class="text-xl font-semibold mb-4">Twitch</h2>
    <HorizontalFormField label="Канал" name="twitch-channel">
      <UInput v-model="state.settings.twitch.channel" @blur="updateSettings" />
    </HorizontalFormField>
  </UForm>
</template>

<style scoped>

</style>