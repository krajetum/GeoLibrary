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
try {
  await authStore.init()
} catch (e) {
  // Un init Keycloak fallito non deve impedire il mount: l'app si carica comunque
  // (Home pubblica) e il router-guard gestirà l'eventuale ri-login.
  console.error('Inizializzazione Keycloak fallita:', e)
}

app.use(router)
app.mount('#app')
