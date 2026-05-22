<script setup lang="ts">
import * as v from 'valibot'
import HorizontalFormField from '~/components/HorizontalFormField.vue'
import { useSignalR } from '~/composables/useSignalR'

const { startSignalR, invokeGet, invokeUpdate } = useSignalR()
const { getLanguage } = useLocalization()
const { t } = useI18n()

const appSettingsInfo = ref({ profileName: '', language: 'en' })
const activeRoot = ref('general')
const activeSub = ref('')

const rootItems = computed(() => [
  { key: 'general', label: t('settings.general.title') },
  { key: 'appearance', label: t('settings.appearance.title') },
  { key: 'platforms', label: t('settings.platforms.title') },
])

const subItemsMap = computed(() => ({
  appearance: [
    { key: 'mainWindow', label: t('settings.appearance.mainWindow.title') },
    { key: 'overlay', label: t('settings.appearance.overlay.title') },
  ],
  platforms: [
    { key: 'twitch', label: t('settings.twitch.name') },
    { key: 'vkVideoLive', label: t('settings.vkVideoLive.name') },
  ],
}))

const subItems = computed(
  () => subItemsMap.value[activeRoot.value as keyof typeof subItemsMap.value] ?? []
)

const defaultSubMap: Record<string, string> = {
  appearance: 'mainWindow',
  platforms: 'twitch',
}

function selectRoot(key: string) {
  activeRoot.value = key
  activeSub.value = defaultSubMap[key] ?? ''
}

const schema = v.object({
  settings: v.object({
    twitch: v.object({
      channel: v.pipe(v.string(), v.minLength(1, 'Test'))
    }),
    vkVideoLive: v.object({
      channel: v.pipe(v.string(), v.minLength(1, 'Test'))
    }),
  })
})

const state = reactive({
  settings: {
    twitch: { channel: '' },
    vkVideoLive: { channel: '' }
  }
})

const mainWindowScale = ref(100)
const overlayScale = ref(100)

const chatOverlayUrl = 'http://localhost:5000/chat'

async function copyOverlayUrl() {
  await navigator.clipboard.writeText(chatOverlayUrl)
}

async function changeTwitchChannel() {
  await invokeUpdate('ChangeTwitchChannel', state.settings.twitch.channel)
}

async function changeVkVideoLiveChannel() {
  await invokeUpdate('ChangeVkVideoLiveChannel', state.settings.vkVideoLive.channel)
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

    <!-- Левая панель: корневое меню -->
    <nav class="w-44 border-r border-gray-800 flex flex-col pt-6 px-2 gap-0.5 shrink-0">
      <button
        v-for="item in rootItems"
        :key="item.key"
        class="w-full px-3 py-2 rounded-lg text-sm text-left transition-colors duration-150"
        :class="activeRoot === item.key
          ? 'bg-gray-700 text-white'
          : 'text-gray-400 hover:bg-gray-800 hover:text-gray-200'"
        @click="selectRoot(item.key)"
      >
        {{ item.label }}
      </button>
    </nav>

    <!-- Правая часть: вкладки + содержимое -->
    <div class="flex-1 flex flex-col overflow-hidden">

      <!-- Верхние табы подменю -->
      <div v-if="subItems.length > 0" class="flex gap-1 px-4 pt-4 border-b border-gray-800 shrink-0">
        <button
          v-for="item in subItems"
          :key="item.key"
          class="px-4 py-2 text-sm transition-colors duration-150 border-b-2 -mb-px"
          :class="activeSub === item.key
            ? 'border-gray-300 text-white'
            : 'border-transparent text-gray-400 hover:text-gray-200'"
          @click="activeSub = item.key"
        >
          {{ item.label }}
        </button>
      </div>

      <!-- Содержимое -->
      <main class="flex-1 p-6 overflow-y-auto">

        <!-- Основные -->
        <template v-if="activeRoot === 'general'">
          <div class="space-y-4 max-w-xl">
            <div class="flex items-center gap-3">
              <label class="w-44 text-sm text-gray-400 shrink-0">
                {{ t('settings.general.chatOverlayUrl') }}
              </label>
              <UInput :model-value="chatOverlayUrl" readonly class="flex-1 font-mono text-xs" />
              <UButton
                icon="i-heroicons-clipboard-document"
                variant="ghost"
                color="neutral"
                size="sm"
                :title="t('settings.general.copyToClipboard')"
                @click="copyOverlayUrl"
              />
            </div>
          </div>
        </template>

        <!-- Оформление -->
        <template v-if="activeRoot === 'appearance'">
          <div class="space-y-4 max-w-xl">
            <div v-if="activeSub === 'mainWindow'" class="flex items-center gap-3">
              <label class="w-44 text-sm text-gray-400 shrink-0">
                {{ t('settings.appearance.scale') }}
              </label>
              <USlider v-model="mainWindowScale" :min="50" :max="200" :step="10" class="flex-1" />
              <span class="text-sm text-gray-400 w-10 text-right">{{ mainWindowScale }}%</span>
            </div>
            <div v-if="activeSub === 'overlay'" class="flex items-center gap-3">
              <label class="w-44 text-sm text-gray-400 shrink-0">
                {{ t('settings.appearance.scale') }}
              </label>
              <USlider v-model="overlayScale" :min="50" :max="200" :step="10" class="flex-1" />
              <span class="text-sm text-gray-400 w-10 text-right">{{ overlayScale }}%</span>
            </div>
          </div>
        </template>

        <!-- Платформы -->
        <template v-if="activeRoot === 'platforms'">
          <UForm :schema="schema" :state="state" class="space-y-4 max-w-xl">
            <template v-if="activeSub === 'twitch'">
              <HorizontalFormField :label="t('settings.channel')" name="settings.twitch.channel">
                <UInput v-model="state.settings.twitch.channel" @blur="changeTwitchChannel" />
              </HorizontalFormField>
            </template>
            <template v-if="activeSub === 'vkVideoLive'">
              <HorizontalFormField :label="t('settings.channel')" name="settings.vkVideoLive.channel">
                <UInput v-model="state.settings.vkVideoLive.channel" @blur="changeVkVideoLiveChannel" />
              </HorizontalFormField>
            </template>
          </UForm>
        </template>

      </main>
    </div>

  </div>
</template>
