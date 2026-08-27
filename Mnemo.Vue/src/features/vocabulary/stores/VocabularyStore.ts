import { defineStore } from 'pinia'
import { ref } from 'vue'
import type {
  CreateEntryRequest,
  VocabularyEntry,
  PatchEntryRequest,
} from '../types/VocabularyEntry'
import { apiRequest } from '@/shared/utils/ApiRequest'
import type {
  VocabularyPage,
  VocabularySector,
  VocabularyStatistics,
} from '../types/VocabularySector'
import { useLoadingPlaceholder } from '@/shared/composables/useLoadingPlaceholder'

export const useVocabularyStore = defineStore('vocabulary', () => {
  const entries = ref<VocabularyEntry[]>([])
  const hasMore = ref<boolean>(true)

  const sectors = ref<VocabularySector[]>([])
  const sectorEntries = ref<number>(0)

  const totalEntries = ref<number>(0)
  const totalTranslations = ref<number>(0)

  const loadingPlaceholder = useLoadingPlaceholder()

  async function fetchPage(
    startWord: string,
    endWord: string,
    page: number,
    pageSize: number = 10,
  ) {
    try {
      const isFirstPage: boolean = page === 1
      loadingPlaceholder.startLoading(!isFirstPage)

      const result = await apiRequest<VocabularyPage>(
        `/api/vocabulary/entries?startWord=${startWord}&endWord=${endWord}&page=${page}&pageSize=${pageSize}`,
      )

      if (isFirstPage) entries.value = result.entries
      else entries.value = entries.value.concat(result.entries)

      hasMore.value = result.hasMore
      sectorEntries.value = result.sectorEntries
    } finally {
      loadingPlaceholder.stopLoading()
    }
  }

  async function fetchSectors(isDescending: boolean = false) {
    try {
      loadingPlaceholder.startLoading()
      const result = await apiRequest<VocabularySector[]>(
        `/api/vocabulary/entries/sectors?isDescending=${isDescending}`,
      )

      sectors.value = result
    } finally {
      loadingPlaceholder.stopLoading()
    }
  }

  async function fetchStatistics() {
    try {
      loadingPlaceholder.startLoading()
      const result = await apiRequest<VocabularyStatistics>(`/api/vocabulary/entries/statistics`)

      totalEntries.value = result.totalEntries
      totalTranslations.value = result.totalTranslations
    } finally {
      loadingPlaceholder.stopLoading()
    }
  }

  async function searchEntries(query: string): Promise<VocabularyEntry[]> {
    try {
      loadingPlaceholder.startLoading()
      return await apiRequest<VocabularyEntry[]>(`/api/Vocabulary/entries/search?query=${query}`)
    } finally {
      loadingPlaceholder.stopLoading()
    }
  }

  async function addEntry(body: CreateEntryRequest) {
    const result = await apiRequest<VocabularyEntry>('/api/vocabulary/entries/', {
      method: 'POST',
      body: JSON.stringify(body),
    })

    entries.value.push(result)

    totalEntries.value++
    totalTranslations.value += result.translations.length
  }

  async function patchEntry(id: number, body: PatchEntryRequest) {
    const result = await apiRequest<VocabularyEntry>(`/api/vocabulary/entries/${id}`, {
      method: 'PATCH',
      body: JSON.stringify(body),
    })

    const index = entries.value.findIndex((e) => e.id === result.id)
    if (index !== -1) {
      entries.value.splice(index, 1, result)
    }

    totalTranslations.value -= body.translationsRemove?.length || 0
    totalTranslations.value += body.translationsAdd?.length || 0
  }

  async function deleteEntry(deleteId: number) {
    const translationsCount = entries.value.find((e) => e.id === deleteId)?.translations.length || 0
    entries.value = entries.value.filter((e) => e.id !== deleteId)

    await apiRequest<boolean>(`/api/vocabulary/entries/${deleteId}`, {
      method: 'DELETE',
    })

    totalEntries.value--
    totalTranslations.value -= translationsCount
  }

  return {
    entries,
    hasMore,
    sectors,
    sectorEntries,
    totalEntries,
    totalTranslations,
    loadingPlaceholder,
    addEntry,
    patchEntry,
    deleteEntry,
    fetchPage,
    fetchSectors,
    fetchStatistics,
    searchEntries,
  }
})
