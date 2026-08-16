<script setup lang="ts">
import type { ContextMenuItem } from '../types/ContextMenuItem'

const props = defineProps<{
  isOpen: boolean
  x: number
  y: number
  items: ContextMenuItem[]
  descriptions: string[]
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

function handleItemClick(item: ContextMenuItem) {
  if (item.disabled) return
  item.action()
  emit('close')
}
</script>

<template>
  <Teleport to="body">
    <Transition name="context-fade">
      <div
        v-if="isOpen"
        class="context-menu"
        :style="{ top: y + 'px', left: 12 + x + 'px' }"
        @click.stop
      >
        <header>
          <div v-for="contextItem in props.items" :key="contextItem.label">
            <div
              class="item"
              :class="{ disable: contextItem.disabled }"
              @mousedown.prevent
              @click="handleItemClick(contextItem)"
            >
              <span class="icon">{{ contextItem.icon }}</span>
              <span class="label">{{ contextItem.label }}</span>
            </div>
          </div>
        </header>
        <footer>
          <div class="descriptions">
            <span v-for="descr in descriptions" :key="descr">{{ descr }}.</span>
          </div>
        </footer>
      </div>
    </Transition>
  </Teleport>
</template>

<style lang="scss" scoped>
.context-menu {
  position: fixed;
  user-select: none;

  display: flex;
  flex-direction: column;

  background-color: $cloud-white;
  border-radius: 0px 12px 12px 12px;

  box-shadow: 5px 5px 0px $shadow;

  filter: drop-shadow(0px 0px 8px #bbbbbb4d);
  backdrop-filter: blur(2px);

  min-width: 220px;
  padding: 7px 6px 10px 6px;

  z-index: 9999;

  header {
    display: flex;
    flex-direction: column;

    gap: 1px;

    &::after {
      content: '';

      position: absolute;

      width: 0;
      height: 0;
      border: 8px solid transparent;
      border-top: 8px solid $cloud-white;
      border-right: 8px solid $cloud-white;

      top: 0px;
      left: -12px;

      background-color: transparent;
    }

    .item {
      cursor: pointer;

      display: flex;
      align-items: center;

      border-radius: 8px;

      padding: 4px;

      .icon {
        @include iconize-text;

        color: $shadow;

        margin-left: 8px;
        margin-right: 12px;

        font-size: 21px;
        line-height: 0.8;
      }

      &:hover {
        background-color: $plane-gray;
      }
    }

    .item.disable {
      cursor: default;

      color: $shadow;

      .icon {
        opacity: 65%;
      }
    }
  }

  footer {
    .descriptions {
      display: flex;
      flex-direction: column;

      gap: 3px;

      color: $gray-font;

      margin-top: 6px;
      margin-left: 10px;

      font-size: 15px;
    }
  }
}

.context-fade-enter-active,
.context-fade-leave-active {
  transition: all 0.18s ease;
}
.context-fade-enter-from,
.context-fade-leave-to {
  opacity: 0%;
  transform: scale(0.97);
}
</style>
