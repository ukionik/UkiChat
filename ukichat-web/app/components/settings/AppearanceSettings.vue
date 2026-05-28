<script setup lang="ts">
import MenuTabSettingsItem from '~/components/MenuTabSettingsItem.vue'

const { t } = useI18n()
const { mainWindowScale, overlayScale } = useScaleSettings()
const { mainWindowTheme, overlayTheme } = useThemeSettings()
const { mainWindowMessageHideDelay, overlayMessageHideDelay } = useMessageHideSettings()
const { overlayHideClippedMessages } = useClipSettings()

function hideDelayLabel(val: number) {
  return val === 0
    ? t('settings.appearance.messageHideDelayNever')
    : `${val} ${t('settings.appearance.messageHideDelaySec')}`
}

const themeOptions = computed(() => [
  { label: t('settings.appearance.themeDefault'), value: 'default' as const },
  { label: t('settings.appearance.themeBox'), value: 'box' as const },
])

const activeSub = ref('mainWindow')
</script>

<template>
  <div>
    <div class="flex gap-1 px-4 pt-4 border-b border-gray-800">
      <MenuTabSettingsItem
        :title="t('settings.appearance.mainWindow.title')"
        :active="activeSub === 'mainWindow'"
        @click="activeSub = 'mainWindow'"
      />
      <MenuTabSettingsItem
        :title="t('settings.appearance.overlay.title')"
        :active="activeSub === 'overlay'"
        @click="activeSub = 'overlay'"
      />
    </div>
    <div class="p-6 space-y-4 max-w-xl">
      <template v-if="activeSub === 'mainWindow'">
        <div class="flex items-center gap-3">
          <label class="w-44 text-sm text-gray-400 shrink-0">
            {{ t('settings.appearance.scale') }}
          </label>
          <USlider v-model="mainWindowScale" :min="10" :max="500" :step="10" class="flex-1" />
          <span class="text-sm text-gray-400 w-10 text-right">{{ mainWindowScale }}%</span>
        </div>
        <div class="flex items-center gap-3">
          <label class="w-44 text-sm text-gray-400 shrink-0">
            {{ t('settings.appearance.theme') }}
          </label>
          <USelect v-model="mainWindowTheme" :items="themeOptions" class="flex-1" />
        </div>
        <div class="flex items-center gap-3">
          <label class="w-44 text-sm text-gray-400 shrink-0">
            {{ t('settings.appearance.messageHideDelay') }}
          </label>
          <USlider v-model="mainWindowMessageHideDelay" :min="0" :max="180" :step="1" class="flex-1" />
          <span class="text-sm text-gray-400 w-16 text-right">{{ hideDelayLabel(mainWindowMessageHideDelay) }}</span>
        </div>
      </template>
      <template v-if="activeSub === 'overlay'">
        <div class="flex items-center gap-3">
          <label class="w-44 text-sm text-gray-400 shrink-0">
            {{ t('settings.appearance.scale') }}
          </label>
          <USlider v-model="overlayScale" :min="10" :max="500" :step="10" class="flex-1" />
          <span class="text-sm text-gray-400 w-10 text-right">{{ overlayScale }}%</span>
        </div>
        <div class="flex items-center gap-3">
          <label class="w-44 text-sm text-gray-400 shrink-0">
            {{ t('settings.appearance.theme') }}
          </label>
          <USelect v-model="overlayTheme" :items="themeOptions" class="flex-1" />
        </div>
        <div class="flex items-center gap-3">
          <label class="w-44 text-sm text-gray-400 shrink-0">
            {{ t('settings.appearance.messageHideDelay') }}
          </label>
          <USlider v-model="overlayMessageHideDelay" :min="0" :max="180" :step="1" class="flex-1" />
          <span class="text-sm text-gray-400 w-16 text-right">{{ hideDelayLabel(overlayMessageHideDelay) }}</span>
        </div>
        <div class="flex items-center gap-3">
          <label class="w-44 text-sm text-gray-400 shrink-0">
            {{ t('settings.appearance.hideClippedMessages') }}
          </label>
          <USwitch v-model="overlayHideClippedMessages" />
        </div>
      </template>
    </div>
  </div>
</template>
