import Keycloak from 'keycloak-js'

const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL ?? 'https://localhost:8080',
  realm: import.meta.env.VITE_KEYCLOAK_REALM ?? 'GeoLibrary',
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'geolibrary-frontend',
})

export default keycloak
