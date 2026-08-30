import { useEventListener } from '@vueuse/core'
import { ref } from 'vue'

export function useSelection() {
  const hasSelection = ref<boolean>(false)
  const selectedText = ref<string>('')

  function updateSelection() {
    const text = window.getSelection()?.toString().trim() ?? ''
    selectedText.value = text
    hasSelection.value = text.length > 0
  }

  function clearSelection() {
    const selection = window.getSelection()
    if (selection) {
      selection.removeAllRanges()
    }

    hasSelection.value = false
    selectedText.value = ''
  }

  useEventListener('selectionchange', updateSelection)
  useEventListener('mouseup', updateSelection)

  updateSelection()

  return {
    hasSelection,
    selectedText,
    clearSelection,
  }
}
