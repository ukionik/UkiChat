// https://nuxt.com/docs/api/configuration/nuxt-config
import {resolve} from 'path'

export default defineNuxtConfig({
    ssr: false,
    css: ['~/assets/css/main.css'],
    vite: {
        server: {
            proxy: {
                '/apphub': {
                    target: 'http://localhost:5000',
                    changeOrigin: true,
                    ws: true
                }
            }
        }
    },
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
        strategy: 'no_prefix',
        detectBrowserLanguage: false,
    }
})