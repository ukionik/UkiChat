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

<template>

</template>

<style scoped>

</style>