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

// Компактная инлайновая метка события (донат/биты/награда) — чтобы влезало в строку.
const eventTag = computed(() => {
  const m = props.message
  if (m.donationAmount) return m.donationAmount
  if (m.bits != null) return t('chat.bits', [m.bits])
  if (m.rewardTitle) return m.rewardTitle
  return ''
})

const boxStyle = computed(() => {
  const base: Record<string, string> = {
    padding: `${0.05 * s.value}rem ${0.25 * s.value}rem`,
    fontSize: `${s.value}rem`,
    lineHeight: '1.35',
  }
  if (variant.value === 'mention') base.background = 'rgba(231, 76, 60, 0.18)'
  else if (variant.value === 'event') base.background = 'rgba(46, 204, 113, 0.18)'
  return base
})
</script>

<template>
  <div
    class="irc-msg whitespace-nowrap overflow-hidden text-ellipsis font-mono"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <span v-if="message.replyTo" class="opacity-45">↩@{{ message.replyTo.displayName }} </span>
    <ChatPlatformBadges :message="message" :scale="scale" inline />
    <span class="font-bold align-middle" :style="{ color: message.displayNameColor }">{{ message.displayName }}</span><span class="opacity-50">:</span>
    <span v-if="eventTag" class="text-green-400">[{{ eventTag }}] </span>
    <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                        :revealed="revealed" @link-click="emit('linkClick', $event)" />
  </div>
</template>

<style scoped>
.irc-msg:nth-child(odd) {
  background: rgba(255, 255, 255, 0.04);
}
</style>
