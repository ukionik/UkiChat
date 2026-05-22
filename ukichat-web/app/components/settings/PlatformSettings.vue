<script setup lang="ts">
import * as v from 'valibot'
import MenuTabSettingsItem from '~/components/MenuTabSettingsItem.vue'
import HorizontalFormField from '~/components/HorizontalFormField.vue'

interface Props {
  twitchChannel: string
  vkVideoLiveChannel: string
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'save-twitch': [value: string]
  'save-vk': [value: string]
}>()

const { t } = useI18n()

const activeSub = ref('twitch')

const schema = v.object({
  twitch: v.object({
    channel: v.pipe(v.string(), v.minLength(1, 'Test'))
  }),
  vkVideoLive: v.object({
    channel: v.pipe(v.string(), v.minLength(1, 'Test'))
  }),
})

const state = reactive({
  twitch: { channel: '' },
  vkVideoLive: { channel: '' },
})

watch(() => props.twitchChannel, val => { state.twitch.channel = val }, { immediate: true })
watch(() => props.vkVideoLiveChannel, val => { state.vkVideoLive.channel = val }, { immediate: true })
</script>

<template>
  <div>
    <div class="flex gap-1 px-4 pt-4 border-b border-gray-800">
      <MenuTabSettingsItem
        :title="t('settings.twitch.name')"
        :active="activeSub === 'twitch'"
        @click="activeSub = 'twitch'"
      />
      <MenuTabSettingsItem
        :title="t('settings.vkVideoLive.name')"
        :active="activeSub === 'vkVideoLive'"
        @click="activeSub = 'vkVideoLive'"
      />
    </div>
    <UForm :schema="schema" :state="state" class="p-6 space-y-4 max-w-xl">
      <template v-if="activeSub === 'twitch'">
        <HorizontalFormField :label="t('settings.channel')" name="twitch.channel">
          <UInput v-model="state.twitch.channel" @blur="emit('save-twitch', state.twitch.channel)" />
        </HorizontalFormField>
      </template>
      <template v-if="activeSub === 'vkVideoLive'">
        <HorizontalFormField :label="t('settings.channel')" name="vkVideoLive.channel">
          <UInput v-model="state.vkVideoLive.channel" @blur="emit('save-vk', state.vkVideoLive.channel)" />
        </HorizontalFormField>
      </template>
    </UForm>
  </div>
</template>
