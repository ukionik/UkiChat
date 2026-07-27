import { useI18n } from 'vue-i18n'

export function useLocalization() {
    const { locale, setLocaleMessage } = useI18n()
    const { invokeGet } = useSignalR()

    const getLanguage = async (language: string) => {
        const messagesJson = await invokeGet("GetLanguage", language)
        const messages = JSON.parse(messagesJson)
        setLocaleMessage(language, messages)
        locale.value = language
    }

    return { getLanguage }
}
