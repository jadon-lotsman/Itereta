<script lang="ts" setup>
import { computed, onMounted } from 'vue'
import RepetitionItem from './RepetitionItem/RepetitionItem.vue'
import { useRepetitionStore } from '../stores/RepetitionStore.ts'
import router from '@/router/index.ts'
import { useCalendarStore } from '@/features/calendar/stores/CalendarStore.ts'
import { ROUTE_NAMES } from '@/shared/constants/RouteConst.ts'
import { useLoadingPlaceholder } from '@/shared/composables/useLoadingPlaceholder.ts'

const calendar = useCalendarStore()
const repetition = useRepetitionStore()
const loadingPlaceholder = useLoadingPlaceholder()

const tasks = computed(() => repetition.tasks)
const buttonText = computed(() =>
  repetition.isFinished
    ? 'Back'
    : loadingPlaceholder.showSkeleton.value
      ? 'Finishing...'
      : 'Finish',
)

let lastAction: number = Date.now()

function onSubmitAnswer(id: number, answer: string) {
  const timeNow = Date.now()
  const elapsedTimeMilliseconds = timeNow - lastAction

  repetition.submitAnswer(id, answer, elapsedTimeMilliseconds)
  lastAction = timeNow
}

async function onFinish() {
  if (repetition.isFinished) {
    router.push({ name: ROUTE_NAMES.VOCABULARY })
  } else {
    loadingPlaceholder.startLoading()
    await repetition.finishTasks()
    await calendar.fetchDays()
    loadingPlaceholder.stopLoading()
  }
}

onMounted(async () => {
  if (repetition.tasks.length == 0) await repetition.fetchTasks()
})
</script>

<template>
  <RepetitionItem
    v-for="(task, index) in tasks"
    :key="task.id"
    :list-number="index + 1"
    :task="task"
    :disabled="repetition.isFinished"
    @submit-answer="onSubmitAnswer"
  />
  <button
    class="big-button"
    type="button"
    @click="onFinish"
    :disabled="loadingPlaceholder.isLoading.value"
  >
    {{ buttonText }}
  </button>
</template>

<style lang="scss" scoped></style>
