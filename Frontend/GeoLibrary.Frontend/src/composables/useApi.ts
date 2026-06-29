import keycloak from '@/core/auth/keycloak'

const BASE_URL = import.meta.env.VITE_API_URL

async function apiFetch(path: string, options: RequestInit = {}) {
  await keycloak.updateToken(30)

  return fetch(`${BASE_URL}/api${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${keycloak.token}`,
      ...options.headers,
    },
  })
}

export function useApi() {
  return { apiFetch }
}
