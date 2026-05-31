<script setup lang="ts">
import type { ChatMessage, MessageType, ReplyInfo } from "~/types/ChatMessage";
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
    case "YouTube": return "/images/youtube.svg"
    case "DonationAlerts": return "/images/donation-alerts.svg"
    default: return ""
  }
}

function getMessageStyle() {
  const base: Record<string, string> = { fontSize: `${props.scale}rem` }
  const type = props.message.messageType
  if (type === 'Notification' || type === 'Mention' || type === 'Reply' || type === 'ChannelPointsRedemption'
      || type === 'Donation' || type === 'Subscription' || type === 'Raid' || type === 'Cheer') {
    base.paddingLeft = `${0.375 * props.scale}rem`
  }
  return base
}

function getMessageClass(messageType: MessageType | undefined) {
  if (messageType === 'Notification') return 'bg-gray-50/10 border-l-[3px] border-gray-50 text-gray-400 rounded-r-sm'
  if (messageType === 'Mention') return 'bg-red-500/10 border-l-[3px] border-red-500 text-red-400 rounded-r-sm'
  if (messageType === 'Reply') return 'border-l-[3px] border-gray-500 rounded-r-sm'
  if (messageType === 'ChannelPointsRedemption') return 'bg-purple-500/10 border-l-[3px] border-purple-400 rounded-r-sm'
  if (messageType === 'Donation') return 'bg-green-500/10 border-l-[3px] border-green-400 rounded-r-sm'
  if (messageType === 'Subscription') return 'bg-violet-500/10 border-l-[3px] border-violet-400 rounded-r-sm'
  if (messageType === 'Raid') return 'bg-blue-500/10 border-l-[3px] border-blue-400 rounded-r-sm'
  if (messageType === 'Cheer') return 'bg-cyan-500/10 border-l-[3px] border-cyan-400 rounded-r-sm'
  return ''
}

function getReplyPreview(replyTo: ReplyInfo) {
  return replyTo.messageParts.filter(p => p.type === 'Text').map(p => p.content).join('').trim()
}

const marginRight = computed(() => `${0.25 * props.scale}rem`)

const replyHeaderStyle = computed(() => ({
  fontSize: `${0.8 * props.scale}rem`
}))

const rewardHeaderStyle = computed(() => ({
  fontSize: `${0.8 * props.scale}rem`
}))

const iconStyle = computed(() => ({
  display: "inline",
  height: `${1.25 * props.scale}rem`,
  marginRight: marginRight.value,
}))

const emoteStyle = computed(() => ({
  display: "inline",
  height: `${1.5 * props.scale}rem`
}))
</script>

<template>
  <div class="chat-message break-words"
       :style="getMessageStyle()"
       :class="[getMessageClass(message.messageType), message.messageType === 'Deleted' ? (allowRevealDeleted ? '' : 'opacity-50') : '']"
       @click="toggleRevealDeleted">
    <div v-if="message.rewardTitle" class="flex items-center gap-1 text-purple-400 truncate" :style="rewardHeaderStyle">
      <img src="/images/channel-points.svg" alt="channel points" class="shrink-0" :style="{ height: '1em', width: '1em' }">
      <span class="truncate font-medium">{{ message.rewardTitle }}</span>
      <span v-if="message.rewardCost != null" class="shrink-0 opacity-75">· {{ message.rewardCost }}</span>
    </div>
    <div v-if="message.donationAmount" class="flex items-center gap-1 text-green-400 truncate" :style="rewardHeaderStyle">
      <img src="/images/money.svg" alt="donation" class="shrink-0" :style="{ height: '1em', width: '1em' }">
      <span class="truncate font-semibold">{{ message.donationAmount }}</span>
    </div>
    <div v-if="message.bits != null" class="flex items-center gap-1 text-cyan-400 truncate" :style="rewardHeaderStyle">
      <img src="/images/bits.svg" alt="bits" class="shrink-0" :style="{ height: '1em', width: '1em' }">
      <span class="truncate font-semibold">{{ t('chat.bits', [message.bits]) }}</span>
    </div>
    <div v-if="message.replyTo" class="flex items-center gap-1 text-gray-400 truncate" :style="replyHeaderStyle">
      <span>↩</span>
      <span class="font-semibold shrink-0">@{{ message.replyTo.displayName }}:</span>
      <span class="truncate opacity-75">{{ getReplyPreview(message.replyTo) }}</span>
    </div>
    <div>
      <img :style="iconStyle" :alt="message.platform" :src="getPlatformImage(message.platform)">
      <img :style="iconStyle" v-for="badge in message.badges" :key="badge" :src="badge" alt="badge">
      <span class="font-bold align-middle"
            :style="{ marginRight: marginRight, color: message.displayNameColor }">{{ message.displayName }}:</span>
      <span class="inline">
        <template v-if="message.messageType === 'Deleted'">
          <template v-if="allowRevealDeleted && revealed">
            <template v-for="(part, index) in message.messageParts" :key="index">
              <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
              <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline" :style="emoteStyle">
              <span v-else-if="part.type === 'Link'" class="inline align-middle text-blue-400 cursor-pointer hover:underline"
                    @click.stop="handleLinkClick(part.content)">{{ part.content }}</span>
            </template>
          </template>
          <span v-else class="inline align-middle italic"
                :class="allowRevealDeleted ? 'text-purple-400 cursor-pointer hover:underline' : 'text-gray-300 opacity-90'">{{ t('chat.messageDeleted') }}</span>
        </template>
        <template v-else v-for="(part, index) in message.messageParts" :key="index">
          <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
          <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline" :style="emoteStyle">
          <span v-else-if="part.type === 'Link'" class="inline align-middle text-blue-400 cursor-pointer hover:underline"
                @click="handleLinkClick(part.content)">{{ part.content }}</span>
        </template>
      </span>
    </div>
  </div>
</template>
