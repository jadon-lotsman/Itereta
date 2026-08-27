<script setup lang="ts">
import ContextMenu from './features/contextMenu/components/ContextMenu.vue'
import NotifyComponent from './features/notify/components/NotificationComponent.vue'
</script>

<template>
  <RouterView />
  <ContextMenu />
  <NotifyComponent />
</template>
