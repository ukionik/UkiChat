<template>
  <div>
    <button>{{ t('Settings.Title') }}</button>
    <button @click="changeLanguage('ru')">RU</button>
    <button @click="changeLanguage('en')">EN</button>
<!--    <Icon icon="mdi:cog" width="24" height="24"/>-->
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useLocalization } from '~/composables/useLocalization'

const { startSignalR, changeLanguage } = useLocalization()
const { t } = useI18n()

// Запуск SignalR при монтировании компонента
onMounted(() => {
  startSignalR()
})

</script>