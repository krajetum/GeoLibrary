import { ref, computed } from 'vue'

/**
 * Consenso al tracciamento delle visite.
 *
 * L'app non usa cookie di profilazione, ma salva in localStorage un identificatore
 * pseudonimo (`x-signature`) che viaggia nell'header `X-User-Signature`: serve al
 * backend per non contare più volte la stessa visita nello stesso giorno.
 * È comunque un identificatore non necessario al funzionamento del servizio,
 * quindi va raccolto solo con consenso esplicito e preventivo.
 *
 * Lo stato vive fuori dalla funzione (modulo-singleton, come useApi): tutti i
 * componenti che chiamano useConsent() condividono lo stesso ref reattivo.
 */

export type ConsentStatus = 'granted' | 'denied'

const CONSENT_KEY = 'gl-consent'
/** Chiave dell'identificatore pseudonimo, condivisa con useApi. */
export const SIGNATURE_KEY = 'x-signature'

function readStoredStatus(): ConsentStatus | null {
  const stored = localStorage.getItem(CONSENT_KEY)
  return stored === 'granted' || stored === 'denied' ? stored : null
}

const status = ref<ConsentStatus | null>(readStoredStatus())

/** True quando l'utente ha già espresso una scelta: pilota la visibilità del banner. */
const hasDecided = computed(() => status.value !== null)

/** True solo con consenso esplicito: nessun tracciamento finché non vale true. */
const isTrackingAllowed = computed(() => status.value === 'granted')

function accept() {
  status.value = 'granted'
  localStorage.setItem(CONSENT_KEY, 'granted')
}

/**
 * Rifiuto o revoca. Oltre a registrare la scelta cancella l'identificatore
 * eventualmente già generato: revocare deve avere un effetto reale sui dati locali.
 */
function reject() {
  status.value = 'denied'
  localStorage.setItem(CONSENT_KEY, 'denied')
  localStorage.removeItem(SIGNATURE_KEY)
}

/** Riporta l'utente allo stato "non ha ancora scelto": il banner ricompare. */
function revoke() {
  status.value = null
  localStorage.removeItem(CONSENT_KEY)
  localStorage.removeItem(SIGNATURE_KEY)
}

export function useConsent() {
  return { status, hasDecided, isTrackingAllowed, accept, reject, revoke }
}
