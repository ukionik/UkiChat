type ChatTheme = 'default' | 'box'

const mainWindowTheme = ref<ChatTheme>('default')
const overlayTheme = ref<ChatTheme>('default')

export function useThemeSettings() {
  return {
    mainWindowTheme,
    overlayTheme,
  }
}
