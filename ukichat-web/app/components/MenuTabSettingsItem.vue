<script setup lang="ts">
interface Props {
  title: string
  active?: boolean
}

defineProps<Props>()
</script>

<template>
  <button
    class="px-4 py-2 text-sm transition-colors duration-150 border-b-2 -mb-px"
    :class="active
      ? 'border-gray-300 text-white'
      : 'border-transparent text-gray-400 hover:text-gray-200'"
  >
    {{ title }}
  </button>
</template>
