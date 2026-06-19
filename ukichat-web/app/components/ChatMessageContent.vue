<script setup lang="ts">
import type { ChatMessage } from "~/types/ChatMessage";
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

const props = withDefaults(defineProps<{
  message: ChatMessage
  scale?: number
  allowRevealDeleted?: boolean
  revealed?: boolean
  // Высота эмоутов в rem относительно масштаба (по умолчанию как в большинстве тем).
  emoteScale?: number
}>(), {
  scale: 1,
  allowRevealDeleted: false,
  revealed: false,
  emoteScale: 1.5,
})

const emit = defineEmits<{
  linkClick: [url: string]
}>()

const emoteStyle = computed(() => ({
  display: "inline",
  height: `${props.emoteScale * props.scale}rem`,
}))
</script>

<template>
  <span class="inline">
    <template v-if="message.messageType === 'Deleted'">
      <template v-if="allowRevealDeleted && revealed">
        <template v-for="(part, index) in message.messageParts" :key="index">
          <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
          <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline" :style="emoteStyle">
          <span v-else-if="part.type === 'Link'" class="inline align-middle text-blue-400 cursor-pointer hover:underline"
                @click.stop="emit('linkClick', part.content)">{{ part.content }}</span>
        </template>
      </template>
      <span v-else class="inline align-middle italic"
            :class="allowRevealDeleted ? 'text-purple-400 cursor-pointer hover:underline' : 'text-gray-300 opacity-90'">{{ t('chat.messageDeleted') }}</span>
    </template>
    <template v-else v-for="(part, index) in message.messageParts" :key="index">
      <span v-if="part.type === 'Text'" class="inline align-middle">{{ part.content }}</span>
      <img v-else-if="part.type === 'Emote'" :src="part.content" alt="emote" class="inline" :style="emoteStyle">
      <span v-else-if="part.type === 'Link'" class="inline align-middle text-blue-400 cursor-pointer hover:underline"
            @click="emit('linkClick', part.content)">{{ part.content }}</span>
    </template>
  </span>
</template>
