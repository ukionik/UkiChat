<script setup lang="ts">
import * as v from 'valibot'
import MenuTabSettingsItem from '~/components/MenuTabSettingsItem.vue'
import HorizontalFormField from '~/components/HorizontalFormField.vue'

import type { TwitchAuthStatus } from '~/types/TwitchAuth'

interface Props {
  twitchChannel: string
  twitchShowStreamUptime: boolean
  vkVideoLiveChannel: string
  youTubeChannel: string
  twitchAuth: TwitchAuthStatus
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'save-twitch': [value: string]
  'save-vk': [value: string]
  'save-youtube': [value: string]
  'authorize-twitch': []
  'logout-twitch': []
  'update-twitch-show-stream-uptime': [value: boolean]
}>()

const { t } = useI18n()

const activeSub = ref('twitch')

// Пустой канал — это не ошибка ввода, а штатный способ отключить площадку. Раньше на пустом
// поле валидатор показывал отладочный текст "Test", оставшийся от первой версии формы.
const schema = v.object({
  twitch: v.object({
    channel: v.string()
  }),
  vkVideoLive: v.object({
    channel: v.string()
  }),
  youTube: v.object({
    channel: v.string()
  }),
})

const state = reactive({
  twitch: { channel: '', showStreamUptime: false },
  vkVideoLive: { channel: '' },
  youTube: { channel: '' },
})

watch(() => props.twitchChannel, val => { state.twitch.channel = val }, { immediate: true })
watch(() => props.twitchShowStreamUptime, val => { state.twitch.showStreamUptime = val }, { immediate: true })
watch(() => props.vkVideoLiveChannel, val => { state.vkVideoLive.channel = val }, { immediate: true })
watch(() => props.youTubeChannel, val => { state.youTube.channel = val }, { immediate: true })

// Канал сохранялся ТОЛЬКО по blur. Окно настроек закрывается вместе с WebView2, и браузер
// в этот момент blur уже не шлёт — введённый канал терялся, если окно закрыли, не уводя
// курсор из поля. Чаще всего это ловилось на первом запуске: канал вводят один раз и сразу
// закрывают настройки. Поэтому дублируем сохранение с задержкой после того, как перестали
// печатать, а blur и Enter сохраняют сразу, без ожидания.
const SAVE_DEBOUNCE_MS = 800
const saveTimers: Record<string, ReturnType<typeof setTimeout> | undefined> = {}

type SaveEvent = 'save-twitch' | 'save-vk' | 'save-youtube'

function saveNow(event: SaveEvent, channel: string) {
  clearTimeout(saveTimers[event])
  saveTimers[event] = undefined
  emit(event, channel)
}

// Вешается на update:model-value, а не на watch состояния: watch срабатывал бы и на
// значения, приехавшие с бэкенда, и гонял бы их обратно.
function saveLater(event: SaveEvent, channel: string) {
  clearTimeout(saveTimers[event])
  saveTimers[event] = setTimeout(() => saveNow(event, channel), SAVE_DEBOUNCE_MS)
}

onBeforeUnmount(() => {
  Object.values(saveTimers).forEach(clearTimeout)
})
</script>

<template>
  <div>
    <div class="flex gap-1 px-4 pt-4 border-b border-gray-800">
      <MenuTabSettingsItem
        :title="t('settings.twitch.name')"
        :active="activeSub === 'twitch'"
        icon="/images/twitch.svg"
        @click="activeSub = 'twitch'"
      />
      <MenuTabSettingsItem
        :title="t('settings.vkVideoLive.name')"
        :active="activeSub === 'vkVideoLive'"
        icon="/images/vk-video-live.svg"
        @click="activeSub = 'vkVideoLive'"
      />
      <MenuTabSettingsItem
        :title="t('settings.youtube.name')"
        :active="activeSub === 'youTube'"
        icon="/images/youtube.svg"
        @click="activeSub = 'youTube'"
      />
    </div>
    <UForm :schema="schema" :state="state" class="p-6 space-y-4 max-w-xl">
      <template v-if="activeSub === 'twitch'">
        <HorizontalFormField :label="t('settings.channel')" name="twitch.channel">
          <UInput
            v-model="state.twitch.channel"
            @update:model-value="saveLater('save-twitch', String($event))"
            @blur="saveNow('save-twitch', state.twitch.channel)"
            @keyup.enter="saveNow('save-twitch', state.twitch.channel)"
          />
        </HorizontalFormField>
        <HorizontalFormField :label="t('settings.twitch.showStreamUptime')" name="twitch.showStreamUptime">
          <UCheckbox
            v-model="state.twitch.showStreamUptime"
            @update:model-value="emit('update-twitch-show-stream-uptime', $event)"
          />
        </HorizontalFormField>

        <div class="pt-4 border-t border-gray-800 space-y-3">
          <div>
            <p class="text-sm font-medium text-gray-200">{{ t('settings.twitch.authTitle') }}</p>
            <p class="text-xs text-gray-500 mt-1">{{ t('settings.twitch.authHint') }}</p>
          </div>
          <div class="flex items-center gap-3">
            <template v-if="props.twitchAuth.authorized">
              <span class="text-sm text-green-400">
                {{ t('settings.twitch.authorizedAs', { login: props.twitchAuth.login }) }}
              </span>
              <UButton color="error" variant="soft" size="sm" @click="emit('logout-twitch')">
                {{ t('settings.twitch.logout') }}
              </UButton>
            </template>
            <template v-else>
              <span class="text-sm text-gray-500">{{ t('settings.twitch.notAuthorized') }}</span>
              <UButton color="primary" variant="solid" size="sm" icon="i-simple-icons-twitch"
                       @click="emit('authorize-twitch')">
                {{ t('settings.twitch.authorize') }}
              </UButton>
            </template>
          </div>
        </div>
      </template>
      <template v-if="activeSub === 'vkVideoLive'">
        <HorizontalFormField :label="t('settings.channel')" name="vkVideoLive.channel">
          <UInput
            v-model="state.vkVideoLive.channel"
            @update:model-value="saveLater('save-vk', String($event))"
            @blur="saveNow('save-vk', state.vkVideoLive.channel)"
            @keyup.enter="saveNow('save-vk', state.vkVideoLive.channel)"
          />
        </HorizontalFormField>
      </template>
      <template v-if="activeSub === 'youTube'">
        <HorizontalFormField :label="t('settings.channel')" name="youTube.channel">
          <UInput
            v-model="state.youTube.channel"
            @update:model-value="saveLater('save-youtube', String($event))"
            @blur="saveNow('save-youtube', state.youTube.channel)"
            @keyup.enter="saveNow('save-youtube', state.youTube.channel)"
          />
        </HorizontalFormField>
      </template>
    </UForm>
  </div>
</template>
