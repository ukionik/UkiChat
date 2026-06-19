<script setup lang="ts">
import type { ChatMessage } from "~/types/ChatMessage";
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

const props = defineProps<{
  message: ChatMessage
  scale: number
  allowRevealDeleted: boolean
}>()

const emit = defineEmits<{
  linkClick: [url: string]
}>()

const { revealed, toggleRevealDeleted } = useThemeMessage(props, emit)

const s = computed(() => props.scale)
const gap = computed(() => `${0.5 * s.value}rem`)
const isEvent = computed(() => messageVariant(props.message.messageType) === 'event')
const isMention = computed(() => props.message.messageType === 'Mention')

// Подпись «Super Chat»-блока: сумма доната, иначе биты, иначе награда.
const eventLabel = computed(() => {
  const m = props.message
  if (m.donationAmount) return m.donationAmount
  if (m.bits != null) return t('chat.bits', [m.bits])
  if (m.rewardTitle) return m.rewardCost != null ? `${m.rewardTitle} · ${m.rewardCost}` : m.rewardTitle
  return ''
})
</script>

<template>
  <!-- Super Chat: цветной блок с суммой -->
  <div v-if="isEvent" class="text-white break-words"
       :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
       :style="{ background: '#1565c0', borderRadius: `${0.5 * s}rem`, margin: `${0.2 * s}rem ${0.4 * s}rem`, padding: `${0.4 * s}rem ${0.65 * s}rem`, fontSize: `${s}rem` }"
       @click="toggleRevealDeleted">
    <div class="font-bold flex items-center gap-1">
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.1" />
      <span>{{ message.displayName }}</span>
      <span v-if="eventLabel" class="opacity-90">· {{ eventLabel }}</span>
    </div>
    <div class="leading-snug">
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>

  <!-- Обычная строка на белом фоне -->
  <div v-else class="break-words text-[#0f0f0f] bg-white/95"
       :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
       :style="{ padding: `${0.3 * s}rem ${0.65 * s}rem`, fontSize: `${s}rem`, lineHeight: '1.3', borderBottom: '1px solid rgba(0,0,0,0.06)' }"
       @click="toggleRevealDeleted">
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-gray-500" />
    <div class="flex items-baseline" :style="{ gap }">
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1" />
      <span class="font-semibold whitespace-nowrap" :class="isMention ? 'text-[#c62828]' : 'text-[#606060]'">{{ message.displayName }}</span>
      <span class="min-w-0">
        <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                            :revealed="revealed" @link-click="emit('linkClick', $event)" />
      </span>
    </div>
  </div>
</template>
