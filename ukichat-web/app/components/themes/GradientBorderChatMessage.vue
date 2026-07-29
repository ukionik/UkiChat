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

// Подсветка типовых сообщений цветом текста — тёмный фон, поэтому оттенки яркие.
const textColor = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#ffb266'
  if (variant === 'mention') return '#ff6b7a'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#c58bff'
  if (variant === 'event') return '#69f0ae'
  return 'inherit'
})

const boxStyle = computed(() => ({
  padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
  marginBottom: `${0.45 * s.value}rem`,
  borderRadius: `${0.5 * s.value}rem`,
  fontSize: `${s.value}rem`,
  background: 'rgba(10, 12, 20, 0.9)',
}))
</script>

<template>
  <div
    class="gborder relative break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" dono-class="text-[#69f0ae] font-semibold" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.15" inline />
      <span class="font-bold align-middle" :style="{ marginRight, color: message.displayNameColor }">{{ message.displayName }}:</span>
      <span class="align-middle" :style="{ color: textColor }">
        <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                            :revealed="revealed" @link-click="emit('linkClick', $event)" />
      </span>
    </div>
  </div>
</template>

<style scoped>
.gborder::before {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: inherit;
  padding: 1.5px;
  /* Прямой переход #00eaff → #ff00d4 в середине даёт тусклый серо-фиолетовый
     (углы рамки выглядят грязными), поэтому ведём его через яркие стопы. */
  background: linear-gradient(135deg, #00eaff 0%, #29a4ff 32%, #7c4dff 58%, #c72dff 80%, #ff00d4 100%);
  -webkit-mask: linear-gradient(#000 0 0) content-box, linear-gradient(#000 0 0);
  -webkit-mask-composite: xor;
  mask-composite: exclude;
  pointer-events: none;
}
</style>
