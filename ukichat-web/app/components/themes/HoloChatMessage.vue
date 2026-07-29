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
const marginRight = computed(() => `${0.3 * s.value}rem`)

// Перелив по типу сообщения: каждая группа — своя тройка оттенков, из которой
// собирается радужная плёнка поверх тёмной подложки.
const sheen = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return ['255, 190, 90', '255, 130, 160', '255, 225, 140']
  if (variant === 'mention') return ['255, 90, 120', '255, 140, 190', '255, 180, 120']
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return ['190, 120, 255', '140, 170, 255', '235, 150, 255']
  if (variant === 'event') return ['110, 255, 190', '140, 230, 255', '190, 255, 150']
  return ['255, 120, 200', '120, 200, 255', '160, 255, 220']
})

const boxStyle = computed(() => {
  const [a, b, c] = sheen.value
  return {
    padding: `${0.45 * s.value}rem ${0.75 * s.value}rem`,
    marginBottom: `${0.45 * s.value}rem`,
    borderRadius: `${0.55 * s.value}rem`,
    fontSize: `${s.value}rem`,
    lineHeight: '1.45',
    color: '#f2f0ff',
    // Три слоя: диагональный блик, радужная плёнка и плотная тёмная подложка.
    background: `linear-gradient(115deg, rgba(255, 255, 255, 0.18) 0%, transparent 32%, transparent 68%, rgba(255, 255, 255, 0.1) 100%),
                 linear-gradient(115deg, rgba(${a}, 0.3) 0%, rgba(${b}, 0.28) 45%, rgba(${c}, 0.3) 100%),
                 rgba(14, 11, 24, 0.92)`,
    border: `1px solid rgba(${b}, 0.55)`,
    boxShadow: `0 0 ${0.6 * s.value}rem rgba(${a}, 0.3), inset 0 0 ${0.7 * s.value}rem rgba(255, 255, 255, 0.08)`,
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#c3bede]" dono-class="text-[#8effd0] font-bold"
                     reward-class="text-[#d3a8ff]" bits-class="text-[#9ad8ff]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.1" inline />
      <span class="font-extrabold align-middle" :style="{ marginRight, color: message.displayNameColor,
            textShadow: `0 0 ${0.06 * s}rem rgba(14, 11, 24, 0.9), 0 0 ${0.5 * s}rem rgba(255, 255, 255, 0.4)` }">{{ message.displayName }}:</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
