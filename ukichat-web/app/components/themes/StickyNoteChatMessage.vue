<script setup lang="ts">
import type { ChatMessage } from "~/types/ChatMessage";

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
// Отступ между ником с двоеточием и текстом сообщения
const marginRight = computed(() => `${0.25 * s.value}rem`)

// Цвет бумажки по типу сообщения — как разные стикеры из одной пачки.
// fold — оттенок загнутого уголка, всегда темнее основного.
const paper = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return { bg: '#ffd9a8', fold: '#e8ab5f' }
  if (variant === 'mention') return { bg: '#ffb8b8', fold: '#ea8080' }
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return { bg: '#dcc2ff', fold: '#b28ded' }
  if (variant === 'event') return { bg: '#bdf3c2', fold: '#84d18b' }
  return { bg: '#fff3a8', fold: '#e8d45f' }
})

// Наклон детерминирован длиной ника: случайный угол «прыгал» бы при каждой
// перерисовке списка, а так каждая бумажка стабильно лежит по-своему.
const tilt = computed(() => `${((props.message.displayName.length % 3) - 1) * 0.7}deg`)

const boxStyle = computed(() => {
  const fold = `${1.1 * s.value}rem`
  return {
    padding: `${0.5 * s.value}rem ${0.75 * s.value}rem`,
    marginBottom: `${0.55 * s.value}rem`,
    fontSize: `${s.value}rem`,
    color: '#3a3320',
    // Срезанный уголок собран прямо в фоне — без псевдоэлементов и лишней разметки.
    background: `linear-gradient(315deg, ${paper.value.fold} 0 ${fold}, ${paper.value.bg} ${fold})`,
    boxShadow: `0 ${0.12 * s.value}rem ${0.4 * s.value}rem rgba(0, 0, 0, 0.35)`,
    transform: `rotate(${tilt.value})`,
  }
})
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#7a6b3f]" dono-class="text-[#1b7a3f] font-bold"
                     reward-class="text-[#6d28d9]" bits-class="text-[#0e7490]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.1" inline />
      <span class="font-extrabold align-middle" :style="{ marginRight, color: message.displayNameColor,
            textShadow: `0 0 ${0.05 * s}rem rgba(58, 51, 32, 0.7)` }">{{ message.displayName }}:</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
