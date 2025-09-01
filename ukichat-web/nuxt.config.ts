// https://nuxt.com/docs/api/configuration/nuxt-config
import {resolve} from 'path'

export default defineNuxtConfig({
    // Режим сборки: static, если хочешь просто отдавать файлы
    target: 'static',
    // Путь, откуда будут браться файлы (вроде base URL)
    /*router: {base: './'},*/
    build: {sourcemap: false},
    nitro: {
        // Меняем папку, куда будут складываться публичные файлы
        output: {
            publicDir: resolve(__dirname, '../UkiChat/UkiChat/wwwroot')
        }
    },

    compatibilityDate: '2025-07-15',
    devtools: {enabled: true},
    modules: ['@nuxt/ui', '@nuxt/eslint']
})