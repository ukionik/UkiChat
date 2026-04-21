<script setup lang="ts">
import {useSignalR} from "~/composables/useSignalR";

const {startSignalR, invokeGet, invokeUpdate} = useSignalR()

async function sendTestMessage() {
  await invokeUpdate("SendChatMessage", generateMessage2())
}

async function sendNotification() {
  await invokeUpdate("SendChatMessage", generateNotification())
}

async function sendMention() {
  await invokeUpdate("SendChatMessage", generateMention())
}

function generateMessage1() {
  return {
    platform: "Twitch",
    badges: [
      "https://static-cdn.jtvnw.net/badges/v1/3267646d-33f0-4b17-b3df-f923a41db1d0/3",
      "https://static-cdn.jtvnw.net/badges/v1/3ffa9565-c35b-4cad-800b-041e60659cf2/3"
    ],
    displayName: "Nightbot",
    messageParts: [
      {
        type: "Text",
        content: "Короче мой вам совет. Не лезьте вообще в ретро и ни в коем случае никогда не покупайте консоли. Иначе закончите лудоманством как я, когда чтобы вывести более чётко один грёбаный пиксель вы тратите на это дополнительные 10к 😁Ставишь просто эмуляторы на комп и радуешься жизни, а не вот это всё"
      }
    ]
  }
}

function generateMessage2() {
  return {
    platform: "Twitch",
    badges: [
      "https://static-cdn.jtvnw.net/badges/v1/3158e758-3cb4-43c5-94b3-7639810451c5/3"
    ],
    displayName: "Moberator1",
    messageParts: [
      {
        type: "Text",
        content: "а привет передать? "
      },
      {
        type: "Emote",
        content: "https://static-cdn.jtvnw.net/emoticons/v2/304486324/default/dark/3.0"
      }
    ]
  }
}

function generateNotification() {
  return {
    platform: "Twitch",
    badges: [],
    displayName: "StreamElements",
    displayNameColor: "#FF6B35",
    messageType: "Notification",
    messageParts: [
      {
        type: "Text",
        content: "Moberator1 только что подписался на канал!"
      }
    ]
  }
}

function generateMention() {
  return {
    platform: "Twitch",
    badges: [
      "https://static-cdn.jtvnw.net/badges/v1/3158e758-3cb4-43c5-94b3-7639810451c5/3"
    ],
    displayName: "Nightbot",
    displayNameColor: "#00FF7F",
    messageType: "Mention",
    messageParts: [
      {
        type: "Text",
        content: "@UkiChat привет из чата!"
      }
    ]
  }
}

onMounted(async () => {
  await startSignalR()
})

</script>

<template>
  <div class="flex gap-2 p-4">
    <UButton @click="sendTestMessage">Test Message</UButton>
    <UButton @click="sendNotification">Notification</UButton>
    <UButton @click="sendMention">Mention</UButton>
  </div>
</template>

<style scoped>

</style>