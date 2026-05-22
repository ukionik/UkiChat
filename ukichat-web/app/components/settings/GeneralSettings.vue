<script setup lang="ts">
const { t } = useI18n()

const chatOverlayUrl = 'http://localhost:5000/chat'

async function copyOverlayUrl() {
  await navigator.clipboard.writeText(chatOverlayUrl)
}
</script>

<template>
  <div class="p-6 space-y-4 max-w-xl">
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
