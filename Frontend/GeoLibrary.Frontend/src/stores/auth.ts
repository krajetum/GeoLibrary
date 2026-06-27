import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import keycloak from '@/core/auth/keycloak'
import { useApi } from '@/composables/useApi'

/**
 * Informazioni sull'utente autenticato.
 * I campi base provengono dal token Keycloak; gli altri vengono
 * arricchiti dalla chiamata a GET /profile/me.
 */
export interface UserInfo {
  id: string | null
  username: string | null
  email: string | null
  displayName: string | null
  avatarUrl: string | null
}

const emptyUserInfo = (): UserInfo => ({
  id: null,
  username: null,
  email: null,
  displayName: null,
  avatarUrl: null,
})

export const useAuthStore = defineStore('auth', () => {
  const isAuthenticated = ref(false)
  const token = ref<string | null>(null)
  const userInfo = ref<UserInfo>(emptyUserInfo())

  /** Popola userInfo con i claim presenti nel token Keycloak. */
  function loadFromToken() {
    const claims = keycloak.tokenParsed
    userInfo.value = {
      id: claims?.sub ?? null,
      username: claims?.preferred_username ?? null,
      email: claims?.email ?? null,
      displayName: claims?.name ?? null,
      avatarUrl: null,
    }
  }

  /**
   * Arricchisce userInfo con i dati del backend.
   * Fallisce in modo silenzioso se l'endpoint non è ancora disponibile.
   */
  async function fetchProfile() {
    try {
      const { apiFetch } = useApi()
      const res = await apiFetch('/profile/me')
      if (!res.ok) return
      const profile = (await res.json()) as Partial<UserInfo>
      userInfo.value = { ...userInfo.value, ...profile }
    } catch (e) {
      console.error('Impossibile caricare il profilo utente:', e)
    }
  }

  async function init() {
    const authenticated = await keycloak.init({
      // Ripristina la sessione esistente al reload tramite check SSO in un iframe
      // nascosto: evita il redirect full-page (pagina bianca) e i parametri OIDC
      // lasciati appesi all'URL.
      onLoad: 'check-sso',
      silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
      responseMode: 'query',
      pkceMethod: 'S256',
      checkLoginIframe: false,
      enableLogging: true,
    })
    isAuthenticated.value = authenticated
    token.value = keycloak.token ?? null

    if (authenticated) {
      loadFromToken()
      await fetchProfile()

      // Refresh token ogni 4 minuti (prima dei 5 min di scadenza tipici)
      setInterval(
        async () => {
          await keycloak.updateToken(60)
          token.value = keycloak.token ?? null
        },
        4 * 60 * 1000,
      )
    }
  }

  function login(redirectPath = '/') {
    keycloak.login({ redirectUri: `${window.location.origin}${redirectPath}` })
  }

  function logout() {
    userInfo.value = emptyUserInfo()
    keycloak.logout({ redirectUri: window.location.origin })
  }

  function hasRole(role: string) {
    return keycloak.hasRealmRole(role)
  }

  const username = computed(() => userInfo.value.username)

  return {
    isAuthenticated,
    token,
    userInfo,
    username,
    init,
    login,
    logout,
    hasRole,
    fetchProfile,
  }
})
