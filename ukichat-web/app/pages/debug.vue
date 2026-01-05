<script setup lang="ts">
import {useSignalR} from "~/composables/useSignalR";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()

async function sendTestMessage() {
  await invokeUpdate("SendChatMessage", {
    displayName: "Player 1",
    message: "Здарова!"
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