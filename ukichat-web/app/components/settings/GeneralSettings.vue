<script setup lang="ts">
const { t } = useI18n()
const { mentionNicknames } = useMentionSettings()

const chatOverlayUrl = 'http://localhost:5000/chat'
const copied = ref(false)
let copiedTimer: ReturnType<typeof setTimeout>

const newNick = ref('')

async function copyOverlayUrl() {
  await navigator.clipboard.writeText(chatOverlayUrl)
  copied.value = true
  clearTimeout(copiedTimer)
  copiedTimer = setTimeout(() => { copied.value = false }, 2000)
}

function addNick() {
  const nick = newNick.value.trim().replace(/,+$/, '')
  if (nick && !mentionNicknames.value.includes(nick)) {
    mentionNicknames.value = [...mentionNicknames.value, nick]
  }
  newNick.value = ''
}

function removeNick(nick: string) {
  mentionNicknames.value = mentionNicknames.value.filter(n => n !== nick)
}

function onNickKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter' || e.key === ',') {
    e.preventDefault()
    addNick()
  }
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

    <div class="flex items-start gap-3">
      <label class="w-44 text-sm text-gray-400 shrink-0 pt-2">
        {{ t('settings.general.mentionNicknames') }}
      </label>
      <div class="flex-1 space-y-2">
        <div v-if="mentionNicknames.length" class="flex flex-wrap gap-1.5">
          <span
            v-for="nick in mentionNicknames"
            :key="nick"
            class="inline-flex items-center gap-1 pl-2 pr-1 py-0.5 rounded bg-gray-700 text-sm text-gray-100"
          >
            {{ nick }}
            <button
              class="text-gray-400 hover:text-white leading-none px-0.5"
              @click="removeNick(nick)"
            >×</button>
          </span>
        </div>
        <UInput
          v-model="newNick"
          :placeholder="t('settings.general.mentionNicknamesPlaceholder')"
          @keydown="onNickKeydown"
          @blur="addNick"
        />
      </div>
    </div>
  </div>
</template>
