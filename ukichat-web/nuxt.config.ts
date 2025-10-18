// https://nuxt.com/docs/api/configuration/nuxt-config
import {resolve} from 'path'

export default defineNuxtConfig({
    css: ['~/assets/css/main.css'],
    nitro: {
        // Меняем папку, куда будут складываться публичные файлы
        output: {
            publicDir: resolve(__dirname, '../UkiChat/UkiChat/wwwroot')
        }
    },

    compatibilityDate: '2025-07-15',
    devtools: {enabled: true},
    modules: ['@nuxt/ui', '@nuxt/eslint', '@nuxtjs/i18n'],
    i18n: {
        defaultLocale: 'ru',
    }
})