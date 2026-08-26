<script setup lang="ts">
import { computed, onUnmounted, ref, watch } from 'vue'
import { onMounted } from 'vue'
import { useVocabularyStore } from '../stores/VocabularyStore.ts'
import VocabularyItem from './VocabularyItem/VocabularyItem.vue'
import VocabularyToolbar from './VocabularyToolbar.vue'
import type {
  CreateEntryRequest,
  VocabularyEntry,
  PatchEntryRequest,
} from '../types/VocabularyEntry.ts'
import ItemSkeleton from './VocabularyItem/ItemSkeleton.vue'
import { useNotify } from '@/shared/composables/useNotify.ts'
import { useContextMenu } from '@/shared/composables/useContextMenu.ts'
import ContextMenu from '@/features/contextMenu/components/ContextMenu.vue'
import type { ContextMenuItem } from '@/features/contextMenu/types/ContextMenuItem.ts'
import { format } from 'date-fns'
import VocabularyNavbar from './VocabularyNavbar.vue'

const notify = useNotify()
const vocabulary = useVocabularyStore()

const contextMenu = useContextMenu()

const templateEntry = ref<VocabularyEntry>()
const searched = ref<VocabularyEntry[]>([])
const entries = computed(() => (searched.value.length > 0 ? searched.value : vocabulary.entries))

const currentPage = ref<number>(1)
const currentStartWord = ref<string>('')
const currentEndWord = ref<string>('')

let observer: IntersectionObserver | null = null
const triggerRef = ref<HTMLElement | null>(null)

async function onSearchSubmit(query: string) {
  const trimmed = query.trim()
  if (!trimmed) {
    searched.value = []
    return
  }

  searched.value = await vocabulary.searchEntries(trimmed)

  if (searched.value.length == 0) notify.info(`No matches for '${query}'`)
}

async function onSortSubmit(isDescending: boolean) {
  await vocabulary.fetchSectors(isDescending)

  const firstSector = vocabulary.sectors[0]
  if (firstSector !== undefined) {
    await onSectorSubmit(firstSector.startWord, firstSector.endWord)
  }
}

async function onSectorSubmit(startWord: string, endWord: string) {
  currentStartWord.value = startWord
  currentEndWord.value = endWord

  currentPage.value = 1
  await vocabulary.fetchPage(currentStartWord.value, currentEndWord.value, currentPage.value)
}

async function fetchMore() {
  if (vocabulary.loadingPlaceholder.isLoading || !vocabulary.hasMore) return
  if (currentStartWord.value === '' || currentEndWord.value === '') return

  currentPage.value++
  await vocabulary.fetchPage(currentStartWord.value, currentEndWord.value, currentPage.value)
}

async function onCreateButton() {
  const toggleValue =
    templateEntry.value === undefined
      ? {
          id: -Date.now(),
          partOfSpeech: undefined,
          foreign: '',
          transcription: undefined,
          transcriptionAudioUrl: undefined,
          translations: [],
          examples: [],
          synonyms: [],
          antonyms: [],
          createdAt: '',
        }
      : undefined

  templateEntry.value = toggleValue
}

async function onEntryCreate(bodyRequest: CreateEntryRequest) {
  templateEntry.value = undefined

  await vocabulary.addEntry(bodyRequest)
}

async function onEntryPatch(id: number, bodyRequest: PatchEntryRequest) {
  await vocabulary.patchEntry(id, bodyRequest)
}

async function onEntryDelete(id: number) {
  await vocabulary.deleteEntry(id)
}

async function openContextMenu(event: MouseEvent, entry: VocabularyEntry) {
  if (entry.id < 0) return

  const menuItems: ContextMenuItem[] = [
    // {
    //   label: 'Pin/Unpin',
    //   icon: 'keep',
    //   action: () => console.log('pinned'),
    // },
    // {
    //   label: 'Reset progress',
    //   icon: 'replay',
    //   action: () => console.log('reset'),
    // },
    {
      label: 'Delete entry',
      icon: 'close',
      action: () => onEntryDelete(entry.id),
    },
  ]

  const creatingDate = format(new Date(entry.createdAt), 'd MMM, yyyy')
  const menuDescriptions: string[] = [`Created at ${creatingDate}`]

  contextMenu.openContext(event, menuItems, menuDescriptions)
}

async function setupObserver() {
  if (!triggerRef.value) return

  if (observer) observer.disconnect()

  observer = new IntersectionObserver(
    async (entries) => {
      const [entry] = entries

      if (
        entry !== undefined &&
        entry.isIntersecting &&
        !vocabulary.loadingPlaceholder.isLoading &&
        vocabulary.hasMore
      ) {
        await fetchMore()
      }
    },
    {
      root: null,
      rootMargin: '0px 0px 100px 0px',
      threshold: 0.1,
    },
  )

  observer.observe(triggerRef.value)
}

onMounted(async () => {
  if (vocabulary.entries.length === 0) {
    await vocabulary.fetchStatistics()
    await onSortSubmit(false)
  }

  await setupObserver()
})

onUnmounted(() => {
  if (observer) {
    observer.disconnect()
    observer = null
  }
})

watch(triggerRef, (newVal) => {
  if (newVal) {
    setupObserver()
  }
})
</script>

<template>
  <div class="vocabulary-container">
    <VocabularyToolbar
      :is-loading="vocabulary.loadingPlaceholder.showSkeleton"
      @submit-search="onSearchSubmit"
      @click-create="onCreateButton"
    />

    <VocabularyNavbar
      :is-loading="vocabulary.loadingPlaceholder.showSkeleton"
      :tablets="vocabulary.sectors"
      :disabled-tablets="searched.length > 0"
      @refresh-sort="onSortSubmit"
      @submit-sector="onSectorSubmit"
    />

    <VocabularyItem v-if="templateEntry" :entry="templateEntry" @create="onEntryCreate" />

    <div v-if="vocabulary.loadingPlaceholder.showSkeleton">
      <ItemSkeleton v-for="e in Math.max(4, entries.length)" :key="e" />
    </div>
    <div v-else>
      <VocabularyItem
        v-for="entry in entries"
        :key="entry.id"
        :entry="entry"
        @create="onEntryCreate"
        @patch="onEntryPatch"
        @contextmenu.capture="(e: MouseEvent) => openContextMenu(e, entry)"
      />

      <span
        ref="triggerRef"
        class="more-placeholder"
        v-if="vocabulary.hasMore && searched.length === 0"
        >Loading entries...</span
      >
    </div>

    <ContextMenu
      :is-open="contextMenu.isOpen.value"
      :pos-x="contextMenu.anchorX.value"
      :pos-y="contextMenu.anchorY.value"
      :is-x-offsetted="contextMenu.isXOffsetted.value"
      :is-y-offsetted="contextMenu.isYOffsetted.value"
      :items="contextMenu.menuItems.value"
      :details="contextMenu.menuDetails.value"
      @close="contextMenu.closeContext"
    />
  </div>
</template>

<style lang="scss" scoped>
.vocabulary-container {
  min-height: 100vh;

  .more-placeholder {
    display: block;

    color: $gray-font;

    padding-top: 5px;
    padding-bottom: 30px;

    text-align: center;
  }
}
</style>
