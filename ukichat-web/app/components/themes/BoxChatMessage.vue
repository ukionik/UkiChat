<script setup lang="ts">
import type { ChatMessage, ReplyInfo } from "~/types/ChatMessage";
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

const revealed = ref(false)

function toggleRevealDeleted() {
  if (!props.allowRevealDeleted || props.message.messageType !== 'Deleted') return
  revealed.value = !revealed.value
}

function handleLinkClick(url: string) {
  emit('linkClick', url)
}

function getPlatformImage(platform: string) {
  switch (platform) {
    case "Twitch": return "/images/twitch.svg"
    case "VkVideoLive": return "/images/vk-video-live.svg"
    case "DonationAlerts": return "/images/donation-alerts.svg"
    default: return ""
  }
}

function getReplyPreview(replyTo: ReplyInfo) {
  return replyTo.messageParts.filter(p => p.type === 'Text').map(p => p.content).join('').trim()
}

const s = computed(() => props.scale)
const iconSize = computed(() => `${1.1 * s.value}rem`)
const emoteSize = computed(() => `${1.5 * s.value}rem`)
const replyFontSize = computed(() => `${0.8 * s.value}rem`)
const nameFontSize = computed(() => `${0.9 * s.value}rem`)
const gap = computed(() => `${0.35 * s.value}rem`)

const boxStyle = computed(() => {
  const base = {
    padding: `${0.5 * s.value}rem ${0.65 * s.value}rem`,
    borderRadius: `${0.35 * s.value}rem`,
    marginBottom: `${0.4 * s.value}rem`,
    fontSize: `${s.value}rem`,
    boxShadow: '0 0 4px rgba(0, 0, 0, 0.4)',
  }

  const type = props.message.messageType
  if (type === 'Mention') {
    return { ...base, background: 'linear-gradient(rgba(120, 40, 40, 0.7), rgba(90, 20, 20, 0.8))' }
  }
  if (type === 'Notification') {
    return { ...base, background: 'linear-gradient(rgba(60, 60, 60, 0.6), rgba(40, 40, 40, 0.7))' }
  }
  if (type === 'ChannelPointsRedemption') {
    return { ...base, background: 'linear-gradient(rgba(100, 40, 140, 0.6), rgba(70, 20, 110, 0.7))' }
  }
  if (type === 'Donation') {
    return { ...base, background: 'linear-gradient(rgba(34, 120, 70, 0.6), rgba(20, 90, 50, 0.7))' }
  }
  return { ...base, background: 'linear-gradient(rgba(70, 70, 70, 0.6), rgba(50, 50, 50, 0.7))' }
})
</script>

<template>
  <div
    class="chat-message"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <!-- Название награды за баллы -->
    <div v-if="message.rewardTitle" class="flex items-center gap-1 text-purple-400 truncate mb-1"
         :style="{ fontSize: replyFontSize }">
      <img src="/images/channel-points.svg" alt="channel points" class="shrink-0" :style="{ height: '1em', width: '1em' }">
      <span class="truncate font-medium">{{ message.rewardTitle }}</span>
      <span v-if="message.rewardCost != null" class="shrink-0 opacity-75">· {{ message.rewardCost }}</span>
    </div>

    <!-- Сумма доната -->
    <div v-if="message.donationAmount" class="flex items-center gap-1 text-green-400 truncate mb-1"
         :style="{ fontSize: replyFontSize }">
      <img src="/images/money.svg" alt="donation" class="shrink-0" :style="{ height: '1em', width: '1em' }">
      <span class="truncate font-semibold">{{ message.donationAmount }}</span>
    </div>

    <!-- Ответ на сообщение -->
    <div v-if="message.replyTo" class="flex items-center gap-1 text-gray-400 truncate mb-1"
         :style="{ fontSize: replyFontSize }">
      <span>↩</span>
      <span class="font-semibold shrink-0">@{{ message.replyTo.displayName }}:</span>
      <span class="truncate opacity-75">{{ getReplyPreview(message.replyTo) }}</span>
    </div>

    <!-- Шапка: иконка платформы + бейджи + имя -->
    <div class="flex items-center" :style="{ gap, marginBottom: gap }">
      <img
        :src="getPlatformImage(message.platform)"
        :alt="message.platform"
        :style="{ height: iconSize, width: iconSize, display: 'block', flexShrink: '0' }"
      >
      <img
        v-for="badge in message.badges"
        :key="badge"
        :src="badge"
        alt="badge"
        :style="{ height: iconSize, display: 'block', flexShrink: '0' }"
      >
      <span
        class="font-bold leading-none"
        :style="{ color: message.displayNameColor, fontSize: nameFontSize }"
      >{{ message.displayName }}</span>
    </div>

    <!-- Текст сообщения -->
    <div class="leading-snug break-words">
      <template v-if="message.messageType === 'Deleted'">
        <template v-if="allowRevealDeleted && revealed">
          <template v-for="(part, index) in message.messageParts" :key="index">
            <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
            <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline"
                 :style="{ height: emoteSize }">
            <span v-else-if="part.type === 'Link'"
                  class="inline align-middle text-blue-400 cursor-pointer hover:underline"
                  @click.stop="handleLinkClick(part.content)">{{ part.content }}</span>
          </template>
        </template>
        <span v-else class="inline align-middle italic"
              :class="allowRevealDeleted ? 'text-purple-400 cursor-pointer hover:underline' : 'text-gray-300 opacity-90'">
          {{ t('chat.messageDeleted') }}
        </span>
      </template>
      <template v-else>
        <template v-for="(part, index) in message.messageParts" :key="index">
          <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
          <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline"
               :style="{ height: emoteSize }">
          <span v-else-if="part.type === 'Link'"
                class="inline align-middle text-blue-400 cursor-pointer hover:underline"
                @click="handleLinkClick(part.content)">{{ part.content }}</span>
        </template>
      </template>
    </div>
  </div>
</template>

<style scoped>
.chat-message {
  animation: slideIn 250ms ease;
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(-12px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

</style>
