const mainWindowScale = ref(200)
const overlayScale = ref(250)

export function useScaleSettings() {
  return {
    mainWindowScale,
    overlayScale,
    mainWindowScaleFactor: computed(() => mainWindowScale.value / 100),
    overlayScaleFactor: computed(() => overlayScale.value / 100),
  }
}
