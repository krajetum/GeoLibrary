import keycloak from '@/core/auth/keycloak'

const BASE_URL = import.meta.env.VITE_API_URL

async function apiFetch(path: string, options: RequestInit = {}) {
  // Le pagine pubbliche chiamano l'API senza sessione: senza questo controllo
  // updateToken andrebbe in errore e la chiamata non partirebbe nemmeno.
  if (keycloak.authenticated) {
    await keycloak.updateToken(30)
  }

  // Con FormData il boundary multipart va generato dal browser: niente Content-Type esplicito.
  const isFormData = options.body instanceof FormData

  return fetch(`${BASE_URL}/api${path}`, {
    ...options,
    headers: {
      ...(isFormData ? {} : { 'Content-Type': 'application/json' }),
      ...(keycloak.authenticated ? { Authorization: `Bearer ${keycloak.token}` } : {}),
      ...options.headers,
    },
  })
}

export function useApi() {
  return { apiFetch }
}
