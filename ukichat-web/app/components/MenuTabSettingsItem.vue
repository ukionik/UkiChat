<script setup lang="ts">
interface Props {
  title: string
  active?: boolean
  icon?: string
}

defineProps<Props>()
</script>

<template>
  <button
    class="flex items-center gap-1.5 px-4 py-2 text-sm transition-colors duration-150 border-b-2 -mb-px"
    :class="active
      ? 'border-gray-300 text-white'
      : 'border-transparent text-gray-400 hover:text-gray-200'"
  >
    <img v-if="icon" :src="icon" class="w-4 h-4 object-contain" />
    {{ title }}
  </button>
</template>
