<script setup lang="ts">
import { computed, ref } from 'vue'
import RadioItem from '@/features/launcher/components/LauncherRadioItem.vue'
import router from '@/router'
import { useRepetitionStore } from '@/features/repetition/stores/RepetitionStore'
import { ROUTE_NAMES } from '@/shared/constants/RouteConst'
import { useNotify } from '@/shared/composables/useNotify'
import { useLoadingPlaceholder } from '@/shared/composables/useLoadingPlaceholder'

const notify = useNotify()
const repetition = useRepetitionStore()
const loadingPlaceholder = useLoadingPlaceholder()

const selectedMode = ref<string>('planned')
const buttonText = computed(() =>
  loadingPlaceholder.showSkeleton.value ? 'Generate tasks...' : 'Start',
)

async function onStart() {
  loadingPlaceholder.startLoading()

  try {
    const exists = await repetition.isExists()

    if (exists) {
      router.push({ name: ROUTE_NAMES.REPETITION })
      notify.info('Repetition already exists. Redirecting...')
    } else {
      const success = await repetition.createTasks(selectedMode.value)

      if (success) {
        router.push({ name: ROUTE_NAMES.REPETITION })
      } else {
        notify.failure(
          `Couldn't start repetition with mode '${selectedMode.value}'. If your vocabulary is empty, please add some entries first`,
        )
      }
    }
  } catch {
  } finally {
    loadingPlaceholder.stopLoading()
  }
}
</script>

<template>
  <form @submit.prevent="onStart">
    <div class="mode-grid">
      <RadioItem
        v-model="selectedMode"
        value="fast"
        title="Fast repetition"
        description="Random entries - no interval changes"
      />
      <RadioItem
        v-model="selectedMode"
        value="planned"
        title="Planned at today"
        description="Planned entries that update your intervals"
      />
    </div>

    <button type="submit" class="big-button" :disabled="loadingPlaceholder.isLoading.value">
      {{ buttonText }}
    </button>
  </form>
</template>

<style lang="scss" scoped>
.mode-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(40%, 1fr));

  gap: 10px;
  margin-bottom: 15px;
}
</style>
