const mainWindowMessageHideDelay = ref(0)
const overlayMessageHideDelay = ref(0)

export function useMessageHideSettings() {
  return {
    mainWindowMessageHideDelay,
    overlayMessageHideDelay,
  }
}
