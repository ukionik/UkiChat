<script setup lang="ts">
defineOptions({ inheritAttrs: false })

interface Props {
  title: string
  active?: boolean
}

defineProps<Props>()
</script>

<template>
  <div>
    <button
      v-bind="$attrs"
      class="w-full px-3 py-2 rounded-lg text-sm text-left transition-colors duration-150"
      :class="active
        ? 'bg-gray-700 text-white'
        : 'text-gray-400 hover:bg-gray-800 hover:text-gray-200'"
    >
      {{ title }}
    </button>
    <Teleport v-if="active" to="#settings-content">
      <slot />
    </Teleport>
  </div>
</template>
