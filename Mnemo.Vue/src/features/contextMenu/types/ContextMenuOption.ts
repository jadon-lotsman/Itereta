export interface ContextMenuOption {
  icon: string
  label: string
  disabled?: boolean
  action: () => void
}
