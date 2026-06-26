import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'
import App from './App.vue'
import router from './router'
import vuetify from '@/plugins/vuetify'

const app = createApp(App)
const pinia = createPinia()
app.use(pinia)
app.use(vuetify)

const authStore = useAuthStore()
await authStore.init()

app.use(router)
app.mount('#app')
