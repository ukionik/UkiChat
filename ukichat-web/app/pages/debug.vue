<script setup lang="ts">
import {useSignalR} from "~/composables/useSignalR";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()

async function sendTestMessage() {
  await invokeUpdate("SendChatMessage", {
    displayName: "Player 1",
    messageParts: [{
      type: "Text",
      content: "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled"
    }]
  })
}

onMounted(async () => {
  await startSignalR()
})

</script>

<template>
  <UButton @click="sendTestMessage">Test Message</UButton>
</template>

<style scoped>

</style>