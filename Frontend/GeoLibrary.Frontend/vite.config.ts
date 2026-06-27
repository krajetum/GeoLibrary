import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import vuetify from 'vite-plugin-vuetify'


// https://vite.dev/config/
export default defineConfig({
  server: {
    port: 5173,
    // Se la porta è occupata fallisce invece di sceglierne un'altra a caso
    strictPort: true,
  },
  plugins: [
    vue(),
    vueDevTools(),
    // autoImport: importa solo i componenti Vuetify effettivamente usati (treeshaking)
    vuetify({ autoImport: true }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
})
