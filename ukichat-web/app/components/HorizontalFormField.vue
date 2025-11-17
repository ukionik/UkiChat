<script setup lang="ts">
import { defineProps } from 'vue'

interface HorizontalFormFieldProps {
  label?: string
  name: string
  [key: string]: any
}

const props = defineProps<HorizontalFormFieldProps>()
</script>

<template>
  <UFormField :name="props.name" v-slot="{ field }">
    <div class="flex items-center gap-4">
      <label class="w-32 text-sm font-medium">{{ props.label }}</label>
      <slot v-bind="field" />
    </div>
  </UFormField>
</template>