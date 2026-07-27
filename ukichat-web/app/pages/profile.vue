<script setup lang="ts">
import { onMounted } from 'vue'
import { useSignalR } from '~/composables/useSignalR'

const { startSignalR } = useSignalR()
const { t } = useI18n()

onMounted(() => {
  void startSignalR()
})
</script>

<template>
<h1>{{ t('profile.title') }}</h1>
</template>

<style scoped>

</style>
