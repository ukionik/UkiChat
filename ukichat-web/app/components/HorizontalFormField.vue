<script setup lang="ts">
import { defineProps } from 'vue'

interface HorizontalFormFieldProps {
  label: string
  name: string
}

const props = defineProps<HorizontalFormFieldProps>()
</script>

<template>
  <UFormField :name="props.name">
    <div class="flex items-center gap-4">
      <label class="w-32 text-sm font-medium">{{ props.label }}</label>
      <slot/>
    </div>
  </UFormField>
</template>