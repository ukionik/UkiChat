import type { ChatMessage } from '~/types/ChatMessage'

// Окно чата: основное окно приложения или оверлей для OBS.
// Настройки хранятся парами (mainWindow*/overlay*), поэтому весь код ниже
// отличается только тем, какое значение из пары взять.
export type ChatWindowTarget = 'mainWindow' | 'overlay'

const MAX_MESSAGES = 1000

export function useChatWindow(target: ChatWindowTarget) {
    const { startSignalR, invokeGet, on, onConnected } = useSignalR()
    const { getLanguage } = useLocalization()
    const { mainWindowScale, overlayScale, mainWindowScaleFactor, overlayScaleFactor } = useScaleSettings()
    const { mainWindowTheme, overlayTheme } = useThemeSettings()
    const { mainWindowMessageHideDelay, overlayMessageHideDelay } = useMessageHideSettings()
    const { overlayHideClippedMessages } = useClipSettings()

    const isOverlay = target === 'overlay'
    // Выбор значения из пары «основное окно / оверлей».
    const pick = <T>(main: T, overlay: T): T => isOverlay ? overlay : main

    const scale = pick(mainWindowScale, overlayScale)
    const scaleFactor = pick(mainWindowScaleFactor, overlayScaleFactor)
    const theme = pick(mainWindowTheme, overlayTheme)
    const messageHideDelay = pick(mainWindowMessageHideDelay, overlayMessageHideDelay)
    // Обрезка не поместившихся сообщений есть только у оверлея.
    const hideClipped = computed(() => isOverlay && overlayHideClippedMessages.value)

    const appSettingsInfo = ref({
        profileName: '',
        language: 'en',
        twitch: { channel: null },
        vkVideoLive: { channel: null },
    })

    const messages = ref<ChatMessage[]>([])

    // Переопределение темы через ?theme=<тема> для тестирования.
    // Если параметр не указан или тема неизвестна — используется тема из настроек.
    const route = useRoute()
    const themeOverride = computed(() => {
        const q = Array.isArray(route.query.theme) ? route.query.theme[0] : route.query.theme
        return isValidTheme(q) ? q : null
    })
    const effectiveTheme = computed(() => themeOverride.value ?? theme.value)

    // Загрузка состояния с бэкенда. Вызывается при каждом (пере)подключении,
    // чтобы после перезапуска приложения окно подхватило актуальные настройки.
    async function loadState() {
        const t0 = performance.now()
        try {
            appSettingsInfo.value = await invokeGet('GetActiveAppSettingsInfo')
            await getLanguage(appSettingsInfo.value.language)

            const scaleSettings = await invokeGet('GetScaleSettings')
            scale.value = pick(scaleSettings.mainWindowScale, scaleSettings.overlayScale)

            const themeSettings = await invokeGet('GetThemeSettings')
            theme.value = pick(themeSettings.mainWindowTheme, themeSettings.overlayTheme)

            const messageHideSettings = await invokeGet('GetMessageHideSettings')
            messageHideDelay.value = pick(
                messageHideSettings.mainWindowMessageHideDelay,
                messageHideSettings.overlayMessageHideDelay,
            )

            if (isOverlay) {
                const clipSettings = await invokeGet('GetClipSettings')
                overlayHideClippedMessages.value = clipSettings.overlayHideClippedMessages
            }

            console.log(`[chat-window:${target}] state loaded (+${(performance.now() - t0).toFixed(0)}ms)`)
        } catch (err) {
            console.error(`[chat-window:${target}] loadState FAILED (+${(performance.now() - t0).toFixed(0)}ms)`, err)
        }
    }

    // Подписки регистрируются один раз, переживают переподключения
    // и снимаются автоматически при размонтировании компонента.
    on('OnScaleSettingsChanged', (main: number, overlay: number) => {
        scale.value = pick(main, overlay)
    })

    on('OnThemeSettingsChanged', (main: string, overlay: string) => {
        theme.value = pick(main, overlay)
    })

    on('OnMessageHideSettingsChanged', (main: number, overlay: number) => {
        messageHideDelay.value = pick(main, overlay)
    })

    on('OnClipSettingsChanged', (overlay: boolean) => {
        overlayHideClippedMessages.value = overlay
    })

    on('OnChatMessage', (message: ChatMessage) => {
        messages.value = [...messages.value, message].slice(-MAX_MESSAGES)
        if (messageHideDelay.value > 0 && message.id) {
            setTimeout(() => {
                messages.value = messages.value.filter(m => m.id !== message.id)
            }, messageHideDelay.value * 1000)
        }
    })

    on('OnMessageDeleted', (messageId: string) => {
        const msg = messages.value.find(m => m.id === messageId)
        if (msg) msg.messageType = 'Deleted'
    })

    on('OnUserMessagesDeleted', (username: string) => {
        messages.value
            .filter(m => m.displayName.toLowerCase() === username.toLowerCase() && m.messageType !== 'Deleted')
            .forEach(m => m.messageType = 'Deleted')
    })

    onConnected(loadState)

    // Соединение поднимается в фоне и бесконечно повторяет попытки, ждать его не нужно.
    void startSignalR()

    return {
        messages,
        appSettingsInfo,
        scale,
        scaleFactor,
        theme: effectiveTheme,
        hideClipped,
    }
}
