<script setup lang="ts">
import NotificationItem from './NotificationItem.vue'
import { useNotificationStore } from '@/features/notify/stores/NotificationStore'

const store = useNotificationStore()
</script>

<template>
  <div class="notification-container">
    <TransitionGroup name="notification" tag="div" class="notifications-list">
      <NotificationItem
        v-for="notification in store.notifications"
        :key="notification.id"
        :data="notification"
      />
    </TransitionGroup>
  </div>
</template>

<style lang="scss" scoped>
.notification-container {
  position: fixed;
  bottom: 20px;
  left: 0;

  z-index: 9998;

  width: 100%;

  pointer-events: none;
}

.notifications-list {
  display: flex;
  flex-direction: column;
  justify-content: end;

  gap: 10px;
  margin: 0px auto;

  max-width: $layoutWidth;
}

.notification-enter-active {
  transition: all 0.18s ease;
}
.notification-leave-active {
  transition: all 0.28s ease;
}
.notification-enter-from,
.notification-leave-to {
  transform: scale(0.97);
  opacity: 0%;
}
.notification-move {
  transition: transform 0.3s cubic-bezier(0.3, 0.9, 0.2, 1.1);
}
</style>
