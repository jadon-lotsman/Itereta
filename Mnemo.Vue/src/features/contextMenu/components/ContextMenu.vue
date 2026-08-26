<script setup lang="ts">
import type { ContextMenuItem } from '../types/ContextMenuItem'

const props = defineProps<{
  isOpen: boolean
  posX: number
  posY: number
  isXOffsetted: boolean
  isYOffsetted: boolean
  items: ContextMenuItem[]
  details: string[]
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
        :class="{
          'triangle-left-top': !isXOffsetted && !isYOffsetted,
          'triangle-right-top': isXOffsetted && !isYOffsetted,
          'triangle-left-bottom': !isXOffsetted && isYOffsetted,
          'triangle-right-bottom': isXOffsetted && isYOffsetted,
        }"
        :style="{ top: posY + 'px', left: posX + 'px' }"
        @click.stop
      >
        <div class="triangle"></div>
        <header>
          <div v-for="contextItem in props.items" :key="contextItem.label">
            <div
              class="item"
              :class="{ disabled: contextItem.disabled }"
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
            <span v-for="info in details" :key="info">{{ info }}.</span>
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
  border-radius: 12px;

  filter: drop-shadow(0px 0px 10px #bbbbbb4d) drop-shadow(5px 5px 0px $shadow);
  backdrop-filter: blur(2px);

  min-width: 220px;
  padding: 7px 6px 10px 6px;

  z-index: 9999;

  header {
    display: flex;
    flex-direction: column;

    gap: 2px;

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

    .item.disabled {
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

.triangle {
  display: block;
  position: absolute;
  width: 0;
  height: 0;
  background-color: transparent;
}

@mixin triangle-corner($v, $h) {
  .triangle {
    border: 8px solid transparent;
    border-#{$v}: 8px solid $cloud-white;
    @if $h == left {
      border-right: 8px solid $cloud-white;
    } @else {
      border-left: 8px solid $cloud-white;
    }
    #{$v}: 0px;
    #{$h}: -12px;
  }
}

.triangle-left-top {
  border-top-left-radius: 0 !important;
  @include triangle-corner(top, left);
}
.triangle-right-top {
  border-top-right-radius: 0 !important;
  @include triangle-corner(top, right);
}
.triangle-left-bottom {
  border-bottom-left-radius: 0 !important;
  @include triangle-corner(bottom, left);
}
.triangle-right-bottom {
  border-bottom-right-radius: 0 !important;
  @include triangle-corner(bottom, right);
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
