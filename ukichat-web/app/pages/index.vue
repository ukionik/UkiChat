<script setup lang="ts">
import { onMounted } from 'vue'

const { t } = useI18n()

// Запуск SignalR при монтировании компонента
onMounted(() => {
  //startSignalR()
})

</script>

<template>
  <div>
    <button>{{ t('settings.title') }}</button>
<!--    <button @click="changeLanguage('ru')">RU</button>
    <button @click="changeLanguage('en')">EN</button>-->
<!--    <Icon icon="mdi:cog" width="24" height="24"/>-->
  </div>
</template>

