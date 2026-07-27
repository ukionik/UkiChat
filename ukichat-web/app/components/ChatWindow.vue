<script setup lang="ts">
// Готовое окно чата: соединение с бэкендом, подписки на события,
// перечитывание настроек после каждого переподключения и рендер сообщений.
// Странице остаётся только указать, какое это окно.
const props = withDefaults(defineProps<{
  target: 'mainWindow' | 'overlay'
  hideVerticalScrollbar?: boolean
  allowRevealDeleted?: boolean
}>(), {
  hideVerticalScrollbar: false,
  allowRevealDeleted: false,
})

const emit = defineEmits<{
  linkClick: [url: string]
}>()

const { messages, scaleFactor, theme, hideClipped } = useChatWindow(props.target)
</script>

<template>
  <ChatContainer
    :messages="messages"
    :scale="scaleFactor"
    :theme="theme"
    :hide-clipped="hideClipped"
    :hide-vertical-scrollbar="hideVerticalScrollbar"
    :allow-reveal-deleted="allowRevealDeleted"
    @link-click="emit('linkClick', $event)"
  />
</template>
