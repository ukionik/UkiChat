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
const variant = computed(() => messageVariant(props.message.messageType))

const eventTag = computed(() => {
  const m = props.message
  if (m.donationAmount) return m.donationAmount
  if (m.bits != null) return t('chat.bits', [m.bits])
  if (m.rewardTitle) return m.rewardTitle
  return ''
})

const boxStyle = computed(() => {
  const base: Record<string, string> = {
    padding: `${0.45 * s.value}rem ${0.6 * s.value}rem`,
    marginBottom: `${0.45 * s.value}rem`,
    borderRadius: `${0.25 * s.value}rem`,
    fontSize: `${s.value}rem`,
    background: 'rgba(10, 5, 25, 0.7)',
    border: '1px solid #0ff',
    boxShadow: '0 0 8px rgba(0,255,255,0.5), inset 0 0 6px rgba(0,255,255,0.15)',
  }
  if (variant.value === 'mention') {
    base.border = '1px solid #f0f'
    base.boxShadow = '0 0 8px rgba(255,0,255,0.6), inset 0 0 6px rgba(255,0,255,0.15)'
  } else if (variant.value === 'event') {
    base.border = '1px solid #0f8'
    base.boxShadow = '0 0 8px rgba(0,255,136,0.6), inset 0 0 6px rgba(0,255,136,0.15)'
  }
  return base
})
</script>

<template>
  <div
    class="font-mono break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <div v-if="message.replyTo" class="text-sm opacity-85">&gt; reply @{{ message.replyTo.displayName }}</div>
    <div v-if="eventTag" class="text-[#0f8] text-sm">[ {{ eventTag }} ]</div>
    <span class="font-bold" :style="{ color: message.displayNameColor, textShadow: '0 0 6px currentColor' }">[{{ message.displayName }}]</span>
    <span> </span>
    <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                        :revealed="revealed" @link-click="emit('linkClick', $event)" />
  </div>
</template>
