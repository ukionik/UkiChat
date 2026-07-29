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
// Отступ между ником и текстом сообщения
const marginRight = computed(() => `${0.3 * s.value}rem`)

// Акцент типа — цвет пиксельной рамки и ника. Палитра ограниченная,
// как у 8-битных консолей: чистые насыщенные цвета без полутонов.
const accent = computed(() => {
  const type = props.message.messageType
  const variant = messageVariant(type)
  if (type === 'Notification' || type === 'Raid') return '#ffa300'
  if (variant === 'mention') return '#ff004d'
  if (type === 'ChannelPointsRedemption' || type === 'Subscription' || type === 'Cheer') return '#a86bff'
  if (variant === 'event') return '#00e436'
  return '#29adff'
})

// Рамка набрана четырьмя тенями-«блоками» вместо border: получается ступенчатый
// контур без сглаженных углов — то, чего border-radius:0 сам по себе не даёт.
const boxStyle = computed(() => {
  const px = `${0.2 * s.value}rem`
  return {
    padding: `${0.45 * s.value}rem ${0.7 * s.value}rem`,
    margin: `0 ${px} ${0.6 * s.value}rem`,
    // Press Start 2P сильно крупнее Inter в том же кегле и шире по шагу, поэтому
    // размер уменьшен, а межстрочный интервал увеличен — иначе строки слипаются.
    fontSize: `${0.68 * s.value}rem`,
    fontFamily: '"Press Start 2P", "Consolas", monospace',
    lineHeight: '1.85',
    color: '#e8e8f0',
    background: '#171728',
    boxShadow: `0 -${px} 0 0 ${accent.value}, 0 ${px} 0 0 ${accent.value},
                -${px} 0 0 0 ${accent.value}, ${px} 0 0 0 ${accent.value}`,
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
    <ChatMessageMeta :message="message" :scale="scale" reply-class="text-[#8b93b8]" dono-class="text-[#00e436] font-bold"
                     reward-class="text-[#a86bff]" bits-class="text-[#29adff]" />
    <div>
      <ChatPlatformBadges :message="message" :scale="scale" :icon-scale="1" inline />
      <!-- Без font-bold: у шрифта одно начертание, браузер синтезировал бы жирный
           и «замазывал» пиксельную сетку глифов. -->
      <span class="align-middle uppercase" :style="{ marginRight, color: accent }">{{ message.displayName }}</span>
      <ChatMessageContent :message="message" :scale="scale" :allow-reveal-deleted="allowRevealDeleted"
                          :revealed="revealed" @link-click="emit('linkClick', $event)" />
    </div>
  </div>
</template>
