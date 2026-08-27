import { useEventListener } from '@vueuse/core'
import { onMounted, ref } from 'vue'

export function useSelection() {
  const hasSelection = ref<boolean>(false)
  const selectedText = ref<string>('')

  function updateSelection() {
    const text = window.getSelection()?.toString().trim() ?? ''
    selectedText.value = text
    hasSelection.value = text.length > 0
  }

  onMounted(() => updateSelection())
  useEventListener('selectionchange', updateSelection)
  useEventListener('mouseup', updateSelection)

  return {
    hasSelection,
    selectedText,
  }
}
