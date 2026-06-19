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

const boxStyle = computed(() => {
  const base: Record<string, string> = {
    padding: `${0.3 * s.value}rem ${0.5 * s.value}rem`,
    fontSize: `${s.value}rem`,
    lineHeight: '1.35',
  }
  const variant = messageVariant(props.message.messageType)
  if (variant === 'mention') {
    base.background = 'rgba(231, 76, 60, 0.15)'
    base.borderLeft = `${0.18 * s.value}rem solid #e74c3c`
    base.paddingLeft = `${0.38 * s.value}rem`
  } else if (variant === 'event') {
    base.background = 'rgba(145, 70, 255, 0.18)'
    base.borderLeft = `${0.18 * s.value}rem solid #9146ff`
    base.paddingLeft = `${0.38 * s.value}rem`
  }
  return base
})
</script>

<template>
  <div
    class="break-words"
    :class="message.messageType === 'Deleted' && !allowRevealDeleted ? 'opacity-50' : ''"
    :style="boxStyle"
    @click="toggleRevealDeleted"
  >
    <ChatMessageMeta :message="message" :scale="scale" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1.15" inline />
      <span class="font-bold align-middle" :style="{ color: message.displayNameColor }">{{ message.displayName }}</span><span class="align-middle opacity-60">: </span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
