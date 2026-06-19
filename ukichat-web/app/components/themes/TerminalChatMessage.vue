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

const textColor = computed(() => {
  if (variant.value === 'mention') return '#ff5577'
  if (variant.value === 'event') return '#ffe14d'
  return '#3eff7e'
})

const boxStyle = computed(() => ({
  padding: `${0.15 * s.value}rem ${0.4 * s.value}rem`,
  fontSize: `${s.value}rem`,
  lineHeight: '1.4',
  color: textColor.value,
  background: 'rgba(0, 0, 0, 0.75)',
}))
</script>

<template>
  <div
    class="font-mono break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <div v-if="message.replyTo" class="opacity-80">&gt;&gt; @{{ message.replyTo.displayName }}</div>
    <div v-if="eventTag" class="opacity-80">$ donate {{ eventTag }}</div>
    <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1" inline /><span class="opacity-50">:~$</span>
    <span class="font-bold"> {{ message.displayName }}</span><span class="opacity-50">&gt;</span>
    <span> </span>
    <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                        :revealed="revealed" @link-click="emit('linkClick', $event)" />
  </div>
</template>
