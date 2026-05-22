<script setup lang="ts">
const { t } = useI18n()

const chatOverlayUrl = 'http://localhost:5000/chat'
const copied = ref(false)
let copiedTimer: ReturnType<typeof setTimeout>

async function copyOverlayUrl() {
  await navigator.clipboard.writeText(chatOverlayUrl)
  copied.value = true
  clearTimeout(copiedTimer)
  copiedTimer = setTimeout(() => { copied.value = false }, 2000)
}
</script>

<template>
  <div class="p-6 space-y-4 max-w-xl">
    <div class="flex items-center gap-3">
      <label class="w-44 text-sm text-gray-400 shrink-0">
        {{ t('settings.general.chatOverlayUrl') }}
      </label>
      <div class="relative flex flex-1">
        <UInput
          :model-value="chatOverlayUrl"
          readonly
          class="flex-1 font-mono text-xs [&_input]:rounded-r-none"
        />
        <UButton
          icon="i-heroicons-clipboard-document"
          variant="subtle"
          color="neutral"
          class="rounded-l-none border border-l-0 border-gray-700"
          :title="t('settings.general.copyToClipboard')"
          @click="copyOverlayUrl"
        />
        <span
          class="absolute left-full ml-3 top-1/2 -translate-y-1/2 text-sm text-green-500 whitespace-nowrap transition-opacity duration-300"
          :class="copied ? 'opacity-100' : 'opacity-0'"
        >
          {{ t('settings.general.copiedToClipboard') }}
        </span>
      </div>
    </div>
  </div>
</template>
