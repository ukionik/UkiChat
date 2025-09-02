// @ts-check
import withNuxt from './.nuxt/eslint.config.mjs'

export default withNuxt(
    js.configs.recommended,
    ...nuxt(),
    {
        files: ['pages/**/*.vue', 'layouts/**/*.vue'],
        rules: {
            'vue/multi-word-component-names': 'off'
        }
    }
)
