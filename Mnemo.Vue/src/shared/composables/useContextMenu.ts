import type { ContextMenuOption } from '@/features/contextMenu/types/ContextMenuOption'
import { ref, nextTick } from 'vue'
import { useActiveInput } from '@/shared/composables/useActiveInput.ts'
import { useSelection } from '@/shared/composables/useSelection.ts'
import { useEventListener } from '@vueuse/core'

const MENU_WIDTH = 220
const MENU_OFFSET = 12
const MENU_PADDING = 10
const MENU_ITEM_HEIGHT = 30

const anchorX = ref<number>(0)
const anchorY = ref<number>(0)
const isVisible = ref<boolean>(false)
const isLeftAligned = ref<boolean>(false)
const isTopAligned = ref<boolean>(false)

const menuOptions = ref<ContextMenuOption[]>([])
const menuDetails = ref<string[]>([])

function getMenuHeight(items: ContextMenuOption[], details: string[]) {
  return MENU_PADDING + (items.length + details.length) * MENU_ITEM_HEIGHT
}

export function useContextMenu() {
  const { hasActiveInput } = useActiveInput()
  const { hasSelection } = useSelection()

  async function openMenu(event: MouseEvent, items: ContextMenuOption[], details: string[]) {
    if (hasActiveInput.value || hasSelection.value) return
    event.preventDefault()
    event.stopPropagation()

    // Next tick to playing animation
    isVisible.value = false
    await nextTick()
    isVisible.value = true

    const MENU_HEIGHT = getMenuHeight(items, details)

    menuOptions.value = items
    menuDetails.value = details

    const cursorX = event.clientX
    const cursorY = event.clientY
    const borderX = window.innerWidth - MENU_WIDTH
    const borderY = window.innerHeight - MENU_HEIGHT

    isLeftAligned.value = cursorX > borderX
    isTopAligned.value = cursorY > borderY
    anchorX.value = isLeftAligned.value ? cursorX - MENU_WIDTH - MENU_OFFSET : cursorX + MENU_OFFSET
    anchorY.value = isTopAligned.value ? cursorY - MENU_HEIGHT : cursorY
  }

  function closeMenu() {
    if (!isVisible.value) return

    isVisible.value = false
    menuOptions.value = []
    menuDetails.value = []
  }

  useEventListener(window, 'click', handleOutsideClick)
  useEventListener(window, 'contextmenu', handleOutsideClick)
  useEventListener(window, 'keydown', handleEscape)
  useEventListener(window, 'scroll', closeMenu)

  function handleOutsideClick(event: MouseEvent) {
    const menu = document.querySelector('.context-menu')
    if (menu && !menu.contains(event.target as Node)) closeMenu()
  }

  function handleEscape(event: KeyboardEvent) {
    if (event.key === 'Escape') closeMenu()
  }

  return {
    anchorX,
    anchorY,
    isVisible,
    isLeftAligned,
    isTopAligned,
    menuOptions,
    menuDetails,
    openMenu,
    closeMenu,
  }
}
