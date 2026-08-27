import { ref } from 'vue'

const currentAudio = ref<HTMLAudioElement | null>(null)
const currentUrl = ref<string | null>(null)

export function useAudioStore() {
  function playAudio(url: string) {
    stopAudio()

    const audio = new Audio(url)
    audio.onended = () => {
      if (currentAudio.value === audio) {
        currentAudio.value = null
        currentUrl.value = null
        audio.onended = null
      }
    }

    currentAudio.value = audio
    currentUrl.value = url

    audio.play().catch((error) => {
      if (error.name === 'AbortError') return

      console.error('Playback failed:', error)
      if (currentAudio.value === audio) {
        stopAudio()
      }
    })
  }

  function isPlayingThis(url: string) {
    return currentUrl.value === url && currentAudio.value !== null && !currentAudio.value.paused
  }

  function stopAudio() {
    if (currentAudio.value) {
      currentAudio.value.pause()
      currentAudio.value.currentTime = 0
      currentAudio.value.onended = null
      currentAudio.value = null
      currentUrl.value = null
    }
  }

  return {
    playAudio,
    stopAudio,
    isPlayingThis,
  }
}
