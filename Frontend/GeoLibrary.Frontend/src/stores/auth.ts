import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import keycloak from '@/core/auth/keycloak'

export const useAuthStore = defineStore('auth', () => {
  const isAuthenticated = ref(false)
  const token = ref<string | null>(null)

  async function init() {
    const authenticated = await keycloak.init({
      checkLoginIframe: false,
    })
    isAuthenticated.value = authenticated
    token.value = keycloak.token ?? null
  }

  function login() {
    keycloak.login({ redirectUri: window.location.origin })
  }

  function logout() {
    keycloak.logout({ redirectUri: window.location.origin })
  }

  function hasRole(role: string) {
    return keycloak.hasRealmRole(role)
  }

  const username = computed(() => keycloak.tokenParsed?.preferred_username ?? null)

  return { isAuthenticated, token, username, init, login, logout, hasRole }
})
