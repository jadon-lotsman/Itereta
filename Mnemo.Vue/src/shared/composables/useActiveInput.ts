import { useEventListener } from '@vueuse/core'
import { ref, computed } from 'vue'

const activeInput = ref<HTMLInputElement | HTMLTextAreaElement | null>(null)
const hasActiveInput = computed(() => activeInput.value != null)

export function useActiveInput() {
  function handleFocus(event: FocusEvent) {
    const target = event.target as HTMLInputElement | HTMLTextAreaElement
    if (target && (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA'))
      activeInput.value = target
  }

  function handleBlur() {
    activeInput.value = null
  }

  useEventListener('focusin', handleFocus)
  useEventListener('focusout', handleBlur)

  return {
    hasActiveInput,
    activeInput,
  }
}
