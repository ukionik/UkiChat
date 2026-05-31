<script setup lang="ts">
import type {ChatMessage} from "~/types/ChatMessage";

const props = withDefaults(defineProps<{
  messages: ChatMessage[]
  scale?: number
  hideVerticalScrollbar?: boolean
  allowRevealDeleted?: boolean
  theme?: 'default' | 'box'
  hideClipped?: boolean
}>(), {
  scale: 1,
  hideVerticalScrollbar: false,
  allowRevealDeleted: false,
  theme: 'default',
  hideClipped: false,
})

const themeComponent = computed(() => {
  return props.theme === 'box' ? resolveComponent('ThemesBoxChatMessage') : resolveComponent('ThemesDefaultChatMessage')
})

const emit = defineEmits<{
  linkClick: [url: string]
}>()

const chatContainer = ref<HTMLElement | null>(null)
const autoScroll = ref(true)

function scrollToBottom() {
  if (!chatContainer.value) return
  chatContainer.value.scrollTop = chatContainer.value.scrollHeight
}

function onScroll() {
  const el = chatContainer.value
  if (!el) return
  const threshold = 200 * props.scale
  autoScroll.value = el.scrollTop + el.clientHeight >= el.scrollHeight - threshold
}

const chatStyle = computed(() => ({
  padding: `${0.25 * props.scale}rem ${0.5 * props.scale}rem`,
}))

const containerClass = computed(() => {
  return props.hideVerticalScrollbar ? 'overflow-y-hidden' : 'overflow-y-auto'
})

// При hideClipped прячем самые старые сообщения, не помещающиеся целиком,
// и сбрасываем скролл в 0. Видимый блок крепится к верху, пустое место — снизу.
function updateClippedVisibility() {
  const el = chatContainer.value
  if (!el) return
  const children = el.children
  // Сначала возвращаем всем display, чтобы корректно измерить высоту.
  for (let i = 0; i < children.length; i++) {
    const child = children[i] as HTMLElement
    child.style.display = ''
    child.style.visibility = ''
  }
  const containerHeight = el.clientHeight
  let total = 0
  let firstVisibleIdx = children.length
  for (let i = children.length - 1; i >= 0; i--) {
    const h = (children[i] as HTMLElement).offsetHeight
    if (total + h > containerHeight) break
    total += h
    firstVisibleIdx = i
  }
  for (let i = 0; i < firstVisibleIdx; i++) {
    (children[i] as HTMLElement).style.display = 'none'
  }
  el.scrollTop = 0
}

function clearClippedVisibility() {
  const el = chatContainer.value
  if (!el) return
  const children = el.children
  for (let i = 0; i < children.length; i++) {
    const child = children[i] as HTMLElement
    child.style.display = ''
    child.style.visibility = ''
  }
}

let resizeObserver: ResizeObserver | null = null

// Применяем текущий layout: либо обрезку старых сообщений, либо автоскролл вниз.
function applyLayout() {
  if (props.hideClipped) {
    updateClippedVisibility()
  } else if (autoScroll.value) {
    scrollToBottom()
  }
}

// Картинки (эмодзи, бейджи, иконки) грузятся асинхронно, поэтому высота сообщений
// сразу после nextTick занижена. Пересчитываем layout повторно по мере догрузки,
// иначе нижнее сообщение «уезжает» за край и выглядит обрезанным/недоскролленным.
const trackedImages = new WeakSet<HTMLImageElement>()

function scheduleLayoutWithImages() {
  applyLayout()
  const el = chatContainer.value
  if (!el) return
  const images = el.querySelectorAll('img')
  images.forEach((img) => {
    if (img.complete || trackedImages.has(img)) return
    trackedImages.add(img)
    const onSettled = () => {
      img.removeEventListener('load', onSettled)
      img.removeEventListener('error', onSettled)
      applyLayout()
    }
    img.addEventListener('load', onSettled)
    img.addEventListener('error', onSettled)
  })
}

watch(() => props.messages, async () => {
  await nextTick()
  scheduleLayoutWithImages()
})

watch(() => props.hideClipped, async (enabled) => {
  await nextTick()
  if (enabled) {
    updateClippedVisibility()
  } else {
    clearClippedVisibility()
    if (autoScroll.value) scrollToBottom()
  }
})

onMounted(() => {
  if (typeof ResizeObserver !== 'undefined' && chatContainer.value) {
    resizeObserver = new ResizeObserver(() => {
      applyLayout()
    })
    resizeObserver.observe(chatContainer.value)
  }
})

onBeforeUnmount(() => {
  resizeObserver?.disconnect()
  resizeObserver = null
})
</script>

<template>
  <div class="chat-container h-dvh overflow-x-hidden" :style="chatStyle" :class="containerClass" ref="chatContainer"
       @scroll="onScroll">
    <component
      :is="themeComponent"
      v-for="message in messages"
      :key="message.id"
      :message="message"
      :scale="scale"
      :allowRevealDeleted="allowRevealDeleted"
      @linkClick="emit('linkClick', $event)"
    />
  </div>
</template>

<style scoped>
.hide-scrollbar {
  scrollbar-width: none;
  -ms-overflow-style: none;
}

.hide-scrollbar::-webkit-scrollbar {
  display: none;
}


</style>
