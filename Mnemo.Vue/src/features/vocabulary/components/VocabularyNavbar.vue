<script setup lang="ts">
import { ref } from 'vue'
import type { VocabularySector } from '../types/VocabularySector'

defineProps<{
  isLoading: boolean
  tablets: VocabularySector[]
  disabledTablets: boolean
}>()

const emit = defineEmits<{
  (e: 'refreshSort', isDescending: boolean): void
  (e: 'submitSector', startWord: string, endWord: string): void
}>()

const sortTimeout = ref<boolean>(false)
const isDescending = ref<boolean>(false)
const pageStartWord = ref<string>('')

function submitSort() {
  if (sortTimeout.value) return
  sortTimeout.value = true

  isDescending.value = !isDescending.value
  emit('refreshSort', isDescending.value)

  setTimeout(() => {
    sortTimeout.value = false
  }, 1000)
}

function submitSector(startWord: string, endWord: string) {
  pageStartWord.value = startWord
  emit('submitSector', startWord, endWord)
}
</script>

<template>
  <div v-show="tablets.length > 0" class="navbar">
    <button
      class="tablet-button"
      :class="{ flipped: isDescending }"
      :disabled="sortTimeout || isLoading || disabledTablets"
      @click="submitSort"
    >
      <span>sort</span>
    </button>

    <div class="tablet-container">
      <label
        class="tablet-radio"
        :class="{ disabled: isLoading || disabledTablets }"
        v-for="tablet in tablets"
        :key="tablet.label"
      >
        <input
          type="radio"
          name="mode"
          :disabled="isLoading || disabledTablets"
          :checked="pageStartWord === tablet.startWord || tablet === tablets[0]"
          @change="submitSector(tablet.startWord, tablet.endWord)"
        />
        <span>{{ tablet.label }}</span>
      </label>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.navbar {
  display: flex;

  gap: 8px;

  margin-bottom: 15px;
  user-select: none;

  .tablet-button,
  .tablet-radio {
    background-color: $plane-white;

    padding: 3px 9px 3px 9px;

    min-width: 35px;
    height: 26px;
    color: $shadow;

    text-align: center;
    white-space: nowrap;
  }

  .tablet-button {
    position: relative;
    flex-grow: 0;

    flex-shrink: 0;

    background-color: $plane-gray;

    width: 45px;

    span {
      @include iconize-text;

      position: absolute;

      top: 2px;
      left: 10px;
    }
  }

  .flipped span {
    transform: scaleY(-1);
  }

  .tablet-container {
    display: flex;
    justify-content: start;

    gap: 8px;

    .tablet-radio {
      cursor: pointer;

      box-shadow: 5px 5px $shadow;

      border-radius: 12px;

      input {
        display: none;
      }

      input:checked + span {
        opacity: 60%;
        color: $black-font;
      }
    }

    .disabled {
      cursor: default;

      background-color: $plane-gray;

      span {
        color: $shadow !important;
      }
    }
  }
}
</style>
