<script setup lang="ts">
import type { ChatMessage } from "~/types/ChatMessage";

const props = withDefaults(defineProps<{
  message: ChatMessage
  scale?: number
  // Высота иконок в rem относительно масштаба.
  iconScale?: number
  // inline=true — иконки в потоке текста (с правым отступом), как в «плоских» темах.
  // inline=false — блочные иконки для flex-шапки, отступы задаёт gap родителя.
  inline?: boolean
}>(), {
  scale: 1,
  iconScale: 1.1,
  inline: false,
})

const iconStyle = computed(() => {
  const size = `${props.iconScale * props.scale}rem`
  if (props.inline) {
    return {
      display: 'inline',
      height: size,
      marginRight: `${0.25 * props.scale}rem`,
      verticalAlign: 'middle',
    }
  }
  return { display: 'block', height: size, flexShrink: '0' }
})
</script>

<template>
  <img :style="iconStyle" :alt="message.platform" :src="getPlatformImage(message.platform)">
  <img v-for="badge in message.badges" :key="badge" :style="iconStyle" :src="badge" alt="badge">
</template>
