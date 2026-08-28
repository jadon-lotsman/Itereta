<script setup lang="ts">
import { capitalize } from '@/shared/utils/StringExtension'
import type { Notification } from '../types/Notification'
import { useNotificationStore } from '../stores/NotificationStore'

const props = defineProps<{ data: Notification }>()

const store = useNotificationStore()

function close() {
  store.removeNotification(props.data.id)
}
</script>

<template>
  <div class="notification" @click="close()">
    <span v-if="data.type === 'success'" class="icon">check_circle</span>
    <span v-else-if="data.type === 'failure'" class="icon">cancel</span>
    <span v-else class="icon">info</span>

    <div>
      <span class="title">{{ capitalize(data.type) }}</span>
      <span class="description">{{
        capitalize(data.message) + (data.message.endsWith('.') ? '' : '.')
      }}</span>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.notification {
  display: flex;
  position: relative;
  align-items: start;

  backdrop-filter: blur(2px);
  filter: drop-shadow(0px 0px 8px #bbbbbb4d);

  will-change: transform, opacity;

  margin-right: 12px;
  margin-left: 15px;

  box-shadow: 5px 5px 0px $shadow;
  border-radius: 0px 12px 12px 12px;

  background-color: $cloud-white;

  padding: 10px 15px;
  padding-right: 25px;

  width: fit-content;
  min-width: 250px;
  max-width: $layoutWidth;

  pointer-events: all;

  &::after {
    position: absolute;

    top: 0px;
    left: -12px;

    border: 8px solid transparent;
    border-top: 8px solid $cloud-white;
    border-right: 8px solid $cloud-white;

    background-color: transparent;

    width: 0;
    height: 0;

    content: '';
  }

  .icon {
    @include iconize-text;

    display: block;

    opacity: 85%;

    margin-top: 5px;
    margin-right: 12px;

    color: $shadow;

    font-size: 32px;
  }

  .title {
    display: block;

    color: $black-font;
  }

  .description {
    display: block;

    margin-top: 3px;

    color: $gray-font;

    font-size: 15px;
  }
}
</style>
