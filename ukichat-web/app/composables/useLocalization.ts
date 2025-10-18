import * as signalR from '@microsoft/signalr'
import { useI18n } from 'vue-i18n'

let connection: signalR.HubConnection | null = null

export function useLocalization() {
    const { locale, setLocaleMessage } = useI18n()

    const startSignalR = async () => {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5000/apphub") // сервер WPF
            .withAutomaticReconnect()
            .build()

        connection.on('LanguageChanged', (culture: string, messagesJson: string) => {
            const messages = JSON.parse(messagesJson)
            setLocaleMessage(culture, messages)
            locale.value = culture
            console.log(`Language switched to ${culture}`)
        })

        await connection.start()
        console.log('SignalR connected')
        // Запросим сразу текущий язык
        await connection.invoke('GetCurrentLanguage')
    }

    const changeLanguage = async (culture: string) => {
        if (connection) {
            console.log(culture)
            await connection.invoke("ChangeLanguage", culture);
        }
    }

    return { startSignalR, changeLanguage }
}