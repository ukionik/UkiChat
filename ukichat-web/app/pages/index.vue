<script setup lang="ts">
const { invokeUpdate } = useSignalR()

// Ссылки открываем во внешнем браузере через WPF-хост, а не внутри WebView2.
function openLink(url: string) {
  invokeUpdate('OpenUrl', url)
}
</script>

<template>
  <ChatWindow target="mainWindow" :allow-reveal-deleted="true" @link-click="openLink" />
</template>
