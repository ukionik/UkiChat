export type ChatTheme = 'default' | 'box'

// Список всех доступных тем чата. При добавлении новой темы — дополнить здесь
// и зарегистрировать соответствующий компонент в ChatContainer.
export const CHAT_THEMES: ChatTheme[] = ['default', 'box']

// Проверка, что значение (например, из query-параметра) — известная тема.
export function isValidTheme(value: unknown): value is ChatTheme {
  return typeof value === 'string' && (CHAT_THEMES as string[]).includes(value)
}

const mainWindowTheme = ref<ChatTheme>('default')
const overlayTheme = ref<ChatTheme>('default')

export function useThemeSettings() {
  return {
    mainWindowTheme,
    overlayTheme,
  }
}
