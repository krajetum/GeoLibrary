import keycloak from '@/core/auth/keycloak'
import { SIGNATURE_KEY, useConsent } from '@/composables/useConsent'

const BASE_URL = import.meta.env.VITE_API_URL

async function apiFetch(path: string, options: RequestInit = {}) {
  // Le pagine pubbliche chiamano l'API senza sessione: senza questo controllo
  // updateToken andrebbe in errore e la chiamata non partirebbe nemmeno.
  if (keycloak.authenticated) {
    await keycloak.updateToken(30)
  }

  // Senza consenso esplicito non generiamo né inviamo l'identificatore pseudonimo:
  // il backend gestisce già l'header assente semplicemente non tracciando la visita.
  const { isTrackingAllowed } = useConsent()
  let signature: string | null = null
  if (isTrackingAllowed.value) {
    signature = localStorage.getItem(SIGNATURE_KEY)
    if (!signature) {
      signature = crypto.randomUUID()
      localStorage.setItem(SIGNATURE_KEY, signature)
    }
  }

  // Con FormData il boundary multipart va generato dal browser: niente Content-Type esplicito.
  const isFormData = options.body instanceof FormData

  return fetch(`${BASE_URL}/api${path}`, {
    ...options,
    headers: {
      ...(isFormData ? {} : { 'Content-Type': 'application/json' }),
      ...(keycloak.authenticated ? { Authorization: `Bearer ${keycloak.token}` } : {}),
      ...(signature ? { 'X-User-Signature': signature } : {}),
      ...options.headers,
    },
  })
}

export function useApi() {
  return { apiFetch }
}
