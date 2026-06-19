<script setup lang="ts">
import type { ChatMessage } from "~/types/ChatMessage";
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

const props = withDefaults(defineProps<{
  message: ChatMessage
  scale?: number
  // Акцентные классы строк (можно переопределить под палитру темы).
  rewardClass?: string
  donoClass?: string
  bitsClass?: string
  replyClass?: string
  // Нижний отступ строк (для «карточных» тем, где блоки разнесены).
  withMargin?: boolean
  // Показывать ли текст-превью отвечаемого сообщения (иначе только @ник).
  showReplyText?: boolean
}>(), {
  scale: 1,
  rewardClass: 'text-purple-400',
  donoClass: 'text-green-400',
  bitsClass: 'text-cyan-400',
  replyClass: 'text-gray-400',
  withMargin: false,
  showReplyText: true,
})

const fontSize = computed(() => `${0.8 * props.scale}rem`)
const marginBottom = computed(() => props.withMargin ? `${0.25 * props.scale}rem` : undefined)
const rowStyle = computed(() => ({ fontSize: fontSize.value, marginBottom: marginBottom.value }))
</script>

<template>
  <!-- Награда за баллы -->
  <div v-if="message.rewardTitle" class="flex items-center gap-1 truncate" :class="rewardClass" :style="rowStyle">
    <img src="/images/channel-points.svg" alt="channel points" class="shrink-0" :style="{ height: '1em', width: '1em' }">
    <span class="truncate font-medium">{{ message.rewardTitle }}</span>
    <span v-if="message.rewardCost != null" class="shrink-0 opacity-75">· {{ message.rewardCost }}</span>
  </div>

  <!-- Сумма доната -->
  <div v-if="message.donationAmount" class="flex items-center gap-1 truncate" :class="donoClass" :style="rowStyle">
    <img src="/images/money.svg" alt="donation" class="shrink-0" :style="{ height: '1em', width: '1em' }">
    <span class="truncate font-semibold">{{ message.donationAmount }}</span>
  </div>

  <!-- Количество бит -->
  <div v-if="message.bits != null" class="flex items-center gap-1 truncate" :class="bitsClass" :style="rowStyle">
    <img src="/images/bits.svg" alt="bits" class="shrink-0" :style="{ height: '1em', width: '1em' }">
    <span class="truncate font-semibold">{{ t('chat.bits', [message.bits]) }}</span>
  </div>

  <!-- Ответ на сообщение -->
  <div v-if="message.replyTo" class="flex items-center gap-1 truncate" :class="replyClass" :style="rowStyle">
    <span>↩</span>
    <span class="font-semibold shrink-0">@{{ message.replyTo.displayName }}<template v-if="showReplyText">:</template></span>
    <span v-if="showReplyText" class="truncate opacity-75">{{ getReplyPreview(message.replyTo) }}</span>
  </div>
</template>
