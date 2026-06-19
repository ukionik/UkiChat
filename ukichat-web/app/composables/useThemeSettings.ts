// Реестр тем чата. Чтобы добавить новую тему: создать компонент в
// app/components/themes/<Name>ChatMessage.vue и добавить запись сюда.
// Всё остальное (выпадающий список в настройках, превью, выбор компонента
// в ChatContainer) строится из этого реестра.

export interface ChatThemeMeta {
  // Идентификатор темы (хранится в настройках, может приходить в ?theme=).
  slug: string
  // Отображаемое имя (бренд-стиль, не переводится).
  label: string
  // Имя авто-импортируемого компонента Nuxt (app/components/themes/...).
  component: string
}

export const CHAT_THEME_LIST: ChatThemeMeta[] = [
  { slug: 'default',     label: 'Default',           component: 'ThemesDefaultChatMessage' },
  { slug: 'box',         label: 'Box',               component: 'ThemesBoxChatMessage' },
  { slug: 'glass',       label: 'Dark Glass',        component: 'ThemesDarkGlassChatMessage' },
  { slug: 'twitch',      label: 'Twitch Native',     component: 'ThemesTwitchNativeChatMessage' },
  { slug: 'youtube',     label: 'YouTube Live',      component: 'ThemesYouTubeLiveChatMessage' },
  { slug: 'outline',     label: 'Minimal Outline',   component: 'ThemesMinimalOutlineChatMessage' },
  { slug: 'neon',        label: 'Neon',              component: 'ThemesNeonChatMessage' },
  { slug: 'bubble',      label: 'Bubble',            component: 'ThemesBubbleChatMessage' },
  { slug: 'irc',         label: 'Compact IRC',       component: 'ThemesCompactIrcChatMessage' },
  { slug: 'accent',      label: 'Accent Bar',        component: 'ThemesAccentBarChatMessage' },
  { slug: 'pastel',      label: 'Pastel Soft',       component: 'ThemesPastelSoftChatMessage' },
  { slug: 'terminal',    label: 'Terminal',          component: 'ThemesTerminalChatMessage' },
  { slug: 'discord',     label: 'Discord',           component: 'ThemesDiscordChatMessage' },
  { slug: 'vapor',       label: 'Vaporwave',         component: 'ThemesVaporwaveChatMessage' },
  { slug: 'material',    label: 'Material Card',     component: 'ThemesMaterialCardChatMessage' },
  { slug: 'comic',       label: 'Comic',             component: 'ThemesComicChatMessage' },
  { slug: 'frost',       label: 'Frosted Glass',     component: 'ThemesFrostedGlassChatMessage' },
  { slug: 'gborder',     label: 'Gradient Border',   component: 'ThemesGradientBorderChatMessage' },
  { slug: 'lower',       label: 'Lower-Third',       component: 'ThemesLowerThirdChatMessage' },
  { slug: 'big',         label: 'Big Casual',        component: 'ThemesBigCasualChatMessage' },
  { slug: 'pinksoft',    label: 'Pink Soft',         component: 'ThemesPinkSoftChatMessage' },
  { slug: 'pinkneon',    label: 'Pink Neon',         component: 'ThemesPinkNeonChatMessage' },
  { slug: 'blackbox',    label: 'Black Box',         component: 'ThemesBlackBoxChatMessage' },
  { slug: 'lightinline', label: 'Light Frost Inline', component: 'ThemesLightFrostInlineChatMessage' },
]

export type ChatTheme = string

// Список всех доступных тем чата.
export const CHAT_THEMES: string[] = CHAT_THEME_LIST.map(t => t.slug)

// Проверка, что значение (например, из query-параметра) — известная тема.
export function isValidTheme(value: unknown): value is ChatTheme {
  return typeof value === 'string' && CHAT_THEMES.includes(value)
}

// Имя компонента для темы; для неизвестной темы — компонент Default.
export function getThemeComponent(slug: string | undefined): string {
  const meta = CHAT_THEME_LIST.find(t => t.slug === slug)
  return (meta ?? CHAT_THEME_LIST[0]!).component
}

const mainWindowTheme = ref<ChatTheme>('default')
const overlayTheme = ref<ChatTheme>('default')

export function useThemeSettings() {
  return {
    mainWindowTheme,
    overlayTheme,
  }
}
