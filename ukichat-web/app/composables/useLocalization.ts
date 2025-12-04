import { useI18n } from 'vue-i18n'
import {HubConnection} from "@microsoft/signalr";

export function useLocalization() {
    const { locale, setLocaleMessage } = useI18n()

    const loadLanguage = async (language: string, connection: HubConnection) => {
        const messagesJson = await connection.invoke("GetLanguage", language)
        const messages = JSON.parse(messagesJson)
        setLocaleMessage(language, messages)
        locale.value = language
        console.log(messages)
    }

    return { loadLanguage }
}