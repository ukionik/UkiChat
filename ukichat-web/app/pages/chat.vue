<script setup lang="ts">
// Оверлей для OBS. Всё общение с бэкендом и рендер сообщений — внутри ChatWindow,
// здесь остаётся только специфичное для оверлея переопределение фона.

// Переопределение фона страницы через ?bg=<цвет>.
// Принимается любой валидный CSS-цвет: transparent, red, %23ff0000 (или просто ff0000),
// rgba(0,0,0,.5) и т.д. Если параметр не указан или значение невалидно —
// остаётся фон по умолчанию из main.css (#000).
function normalizeBackground(value: string | null | undefined): string | null {
  if (!value) return null
  let color = value.trim()
  if (!color) return null
  // Голый hex без решётки: в URL «#» приходится экранировать, поэтому разрешаем и без него.
  if (/^[0-9a-f]{3,8}$/i.test(color) && [3, 4, 6, 8].includes(color.length)) {
    color = `#${color}`
  }
  // Проверка браузером отсекает мусор и попытки подставить произвольный CSS.
  if (typeof CSS === 'undefined' || !CSS.supports('background-color', color)) return null
  return color
}

const route = useRoute()

const backgroundOverride = computed(() => {
  const q = Array.isArray(route.query.bg) ? route.query.bg[0] : route.query.bg
  return normalizeBackground(q)
})

useHead({
  bodyAttrs: {
    style: computed(() => backgroundOverride.value ? `background: ${backgroundOverride.value};` : ''),
  },
})
</script>

<template>
  <ChatWindow target="overlay" :hide-vertical-scrollbar="true" />
</template>
