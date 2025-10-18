<template>
  <div>
    <h2>Сообщения:</h2>
    <ul>
      <li v-for="(msg, i) in messages" :key="i">{{ msg }}</li>
    </ul>
    <button @click="sendMessage">Отправить сообщение</button>
    <button @click="openSettings">Настройки</button>
    <button @click="changeLanguage('ru')">RU</button>
    <button @click="changeLanguage('en')">EN</button>
    <Icon icon="mdi:cog" width="24" height="24"/>
  </div>
</template>

<script setup>
import {Icon} from '@iconify/vue';
import {HubConnectionBuilder} from "@microsoft/signalr";
import {ref, onMounted} from "vue";

const messages = ref([]);
let connection;
onMounted(async () => {
  connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5000/apphub") // сервер WPF
      .withAutomaticReconnect()
      .build();

  connection.on("ReceiveMessage", (user, message) => {
    messages.value.push(`${user}: ${message}`);
  });

  await connection.start();
})

async function sendMessage() {
  if (connection) {
    await connection.invoke("SendMessage", "VueClient", "Привет из Vue!");
  }
}

async function openSettings() {
  if (connection) {
    await connection.invoke("OpenSettingsWindow");
  }
}

async function changeLanguage(lang) {
  if (connection) {
    await connection.invoke("ChangeLanguage", lang);
  }
}

</script>