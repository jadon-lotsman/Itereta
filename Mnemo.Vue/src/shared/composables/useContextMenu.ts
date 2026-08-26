import type { ContextMenuItem } from '@/features/contextMenu/types/ContextMenuItem'
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import { useActiveInput } from '@/shared/composables/useActiveInput.ts'
import { useSelection } from '@/shared/composables/useSelection.ts'

export function useContextMenu() {
  const isOpen = ref<boolean>(false)
  const isXOffsetted = ref<boolean>(false)
  const isYOffsetted = ref<boolean>(false)
  const anchorX = ref<number>(0)
  const anchorY = ref<number>(0)

  const menuItems = ref<ContextMenuItem[]>([])
  const menuDetails = ref<string[]>([])

  const inputChecker = useActiveInput()
  const selectionChecker = useSelection()

  const MENU_WIDTH = 220
  const MENU_OFFSET = 12

  const MENU_ITEM_HEIGHT = 30

  async function openContext(event: MouseEvent, items: ContextMenuItem[], details: string[]) {
    if (inputChecker.hasActiveInput.value || selectionChecker.hasSelection.value) return

    event.preventDefault()
    event.stopPropagation()

    isOpen.value = false
    await nextTick()
    isOpen.value = true

    menuItems.value = items
    menuDetails.value = details

    const MENU_HEIGHT = 10 + (menuItems.value.length + menuDetails.value.length) * MENU_ITEM_HEIGHT

    const cursorX = event.clientX
    const cursorY = event.clientY
    const borderX = window.innerWidth - MENU_WIDTH
    const borderY = window.innerHeight - MENU_HEIGHT

    isXOffsetted.value = cursorX > borderX
    isYOffsetted.value = cursorY > borderY
    anchorX.value = isXOffsetted.value ? cursorX - MENU_WIDTH - MENU_OFFSET : cursorX + MENU_OFFSET
    anchorY.value = isYOffsetted.value ? cursorY - MENU_HEIGHT : cursorY
  }

  async function closeContext() {
    isOpen.value = false
    menuItems.value = []
    menuDetails.value = []
  }

  async function handleGlobalClick(event: MouseEvent) {
    if (!isOpen.value) return

    const menuElement = document.querySelector('.context-menu')
    if (menuElement && !menuElement.contains(event.target as Node)) {
      event.preventDefault()
      closeContext()
    }
  }

  async function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && isOpen.value) {
      closeContext()
    }
  }

  onMounted(() => {
    window.addEventListener('click', handleGlobalClick)
    window.addEventListener('scroll', closeContext)
    window.addEventListener('contextmenu', handleGlobalClick)
    window.addEventListener('keydown', handleKeydown)
  })

  onUnmounted(() => {
    window.removeEventListener('click', handleGlobalClick)
    window.removeEventListener('scroll', closeContext)
    window.removeEventListener('contextmenu', handleGlobalClick)
    window.removeEventListener('keydown', handleKeydown)
  })

  return {
    isOpen,
    isXOffsetted,
    isYOffsetted,
    anchorX,
    anchorY,
    menuItems,
    menuDetails,
    openContext,
    closeContext,
  }
}
