<script setup lang="ts">
defineProps<{
  modelValue: string
  value: string
  title: string
  description: string
}>()

defineEmits<{
  (e: 'update:modelValue', query: string): void
}>()
</script>

<template>
  <label class="mode-radio">
    <input
      type="radio"
      name="mode"
      :value="value"
      :checked="modelValue === value"
      @change="$emit('update:modelValue', value)"
    />
    <div class="title">{{ title }}</div>
    <div class="description">{{ description }}</div>
  </label>
</template>

<style lang="scss" scoped>
.mode-radio {
  @include lift();

  cursor: pointer;

  box-shadow: 5px 5px 0px $shadow;

  user-select: none;

  border-radius: 12px;
  padding: 10px;

  background-color: $plane-white;

  input {
    display: none;
  }

  .title {
    display: flex;
    color: $black-font;

    font-size: 16px;
    padding-bottom: 20px;

    position: relative;

    &::before {
      content: '';
      display: block;

      background-color: $plane-gray;

      border-radius: 4px;
      margin-right: 10px;

      width: 20px;
      height: 20px;
    }

    &::after {
      @include iconize-text;

      content: 'check';
      color: $gray-font;

      opacity: 0%;

      position: absolute;
      top: -3px;
      left: -3px;

      font-size: 26px;
    }
  }

  .description {
    color: $gray-font;

    font-size: 14px;
    font-weight: 400;

    max-width: 90%;
  }

  input:checked + .title::after {
    opacity: 100%;
  }
}
</style>
