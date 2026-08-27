import { ref, onUnmounted } from 'vue'

export function useLoadingPlaceholder(delay: number = 300) {
  const isLoading = ref(false)
  const showSkeleton = ref(false)

  let skeletonTimer: ReturnType<typeof setTimeout> | null = null

  function startLoading(disableSkeleton: boolean = false) {
    isLoading.value = true
    showSkeleton.value = false

    clearSkeletonTimer()

    if (!disableSkeleton) {
      skeletonTimer = setTimeout(() => {
        if (isLoading.value) {
          showSkeleton.value = true
        }
      }, delay)
    }
  }

  function stopLoading() {
    isLoading.value = false
    showSkeleton.value = false
    clearSkeletonTimer()
  }

  function clearSkeletonTimer() {
    if (skeletonTimer) {
      clearTimeout(skeletonTimer)
      skeletonTimer = null
    }
  }

  onUnmounted(clearSkeletonTimer)

  return {
    isLoading,
    showSkeleton,
    startLoading,
    stopLoading,
  }
}
