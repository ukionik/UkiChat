const MAIN_KEY = 'ukichat_scale_main'
const OVERLAY_KEY = 'ukichat_scale_overlay'

const mainWindowScale = ref(200)
const overlayScale = ref(250)

if (import.meta.client) {
  mainWindowScale.value = parseInt(localStorage.getItem(MAIN_KEY) ?? '200')
  overlayScale.value = parseInt(localStorage.getItem(OVERLAY_KEY) ?? '250')

  watch(mainWindowScale, (val) => localStorage.setItem(MAIN_KEY, String(val)))
  watch(overlayScale, (val) => localStorage.setItem(OVERLAY_KEY, String(val)))

  window.addEventListener('storage', (e) => {
    if (e.key === MAIN_KEY && e.newValue !== null) mainWindowScale.value = parseInt(e.newValue)
    if (e.key === OVERLAY_KEY && e.newValue !== null) overlayScale.value = parseInt(e.newValue)
  })
}

export function useScaleSettings() {
  return {
    mainWindowScale,
    overlayScale,
    mainWindowScaleFactor: computed(() => mainWindowScale.value / 100),
    overlayScaleFactor: computed(() => overlayScale.value / 100),
  }
}
