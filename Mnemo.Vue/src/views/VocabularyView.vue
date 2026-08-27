<script setup lang="ts">
import CollapsibleSection from '@/shared/components/CollapsibleSection.vue'
import Calendar from '@/features/calendar/components/CalendarComponent.vue'
import Launcher from '@/features/launcher/components/LauncherComponent.vue'
import Vocabulary from '@/features/vocabulary/components/VocabularyComponent.vue'
import { useVocabularyStore } from '@/features/vocabulary/stores/VocabularyStore'
import { useRouter } from 'vue-router'
import { ROUTE_NAMES } from '@/shared/constants/RouteConst'

window.scrollTo(0, 0)

const router = useRouter()
const vocabulary = useVocabularyStore()

const logout = () => {
  localStorage.removeItem('token')
  router.push({ name: ROUTE_NAMES.LOGIN })
}
</script>

<template>
  <CollapsibleSection title="Calendar">
    <Calendar></Calendar>
  </CollapsibleSection>

  <CollapsibleSection title="Launcher">
    <Launcher></Launcher>
  </CollapsibleSection>

  <CollapsibleSection title="Vocabulary">
    <template #subtitle>
      <span
        >{{ vocabulary.totalEntries }} entries,
        {{ vocabulary.totalTranslations }} translations</span
      >
    </template>

    <Vocabulary></Vocabulary>
  </CollapsibleSection>

  <button @click="logout">logout</button>
</template>

<style lang="scss" scoped>
button {
  @include iconize-text;
  @include lift();

  position: fixed;
  top: 5px;
  left: 5px;
  background-color: $cloud-white;

  color: $shadow;
}
</style>
