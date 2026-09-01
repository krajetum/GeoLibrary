<template>
  <!--
    Informativa privacy. Struttura semantica (h1/h2, liste, dl) e non solo classi
    tipografiche: l'ordine di lettura deve restare comprensibile con uno screen
    reader e con i CSS disattivati.
  -->
  <v-container class="py-8" style="max-width: 860px">
    <h1 class="text-h4 font-weight-bold mb-2">Informativa sul trattamento dei dati personali</h1>
    <p class="text-body-2 text-medium-emphasis mb-6">Ultimo aggiornamento: settembre 2026</p>

    <p class="mb-6">
      GeoLibrary è un prototipo accademico che permette di pubblicare e consultare il patrimonio
      librario di biblioteche private. Questa informativa descrive quali dati vengono trattati, con
      quali finalità e quali scelte restano nelle mani della persona interessata.
    </p>

    <h2 class="text-h6 mt-6 mb-2">1. Titolare del trattamento</h2>
    <p>
      Il titolare è il gestore dell'istanza di GeoLibrary. Trattandosi di un prototipo realizzato a
      fini didattici, i dati inseriti non devono essere considerati destinati a un uso reale.
    </p>

    <h2 class="text-h6 mt-6 mb-2">2. Dati trattati e finalità</h2>
    <dl>
      <dt class="font-weight-medium mt-3">Dati dell'account</dt>
      <dd>
        Nome utente, indirizzo email e nome visualizzato, gestiti dal servizio di autenticazione
        Keycloak. Finalità: consentire l'accesso e attribuire a ogni utente le proprie librerie.
      </dd>

      <dt class="font-weight-medium mt-3">Dati del profilo</dt>
      <dd>
        Nome visualizzato e immagine di profilo, se caricata. Finalità: permettere agli altri utenti
        di riconoscere chi propone un libro in consultazione o in prestito.
      </dd>

      <dt class="font-weight-medium mt-3">Posizione delle librerie pubblicate</dt>
      <dd>
        L'indirizzo indicato al momento della pubblicazione viene convertito in coordinate
        geografiche per consentire la ricerca per distanza e la visualizzazione su mappa.
        <strong>
          A chi non è il proprietario e non ha un prestito approvato in corso, l'applicazione non
          mostra l'indirizzo esatto né il CAP: restano visibili solo comune e nazione, e le
          coordinate vengono arrotondate.
        </strong>
        Le librerie possono inoltre essere contrassegnate come nascoste, e in tal caso non compaiono
        in alcuna ricerca.
      </dd>

      <dt class="font-weight-medium mt-3">Statistiche di consultazione</dt>
      <dd>
        Il numero di visualizzazioni giornaliere di ogni libreria e di ogni libro. Il dato è
        aggregato per giorno e viene mostrato al solo proprietario del patrimonio (e, in forma
        complessiva sull'intera piattaforma, all'amministratore). Per evitare di contare più volte
        la stessa visita viene salvato nel browser un identificatore casuale, inviato al server
        nell'intestazione <code>X-User-Signature</code>. L'identificatore non è collegato
        all'identità della persona, non viene usato per profilazione e la corrispondenza lato server
        scade a fine giornata. <strong>Viene generato solo dopo il consenso esplicito</strong> e
        cancellato in caso di rifiuto o revoca.
      </dd>

      <dt class="font-weight-medium mt-3">Richieste di prestito</dt>
      <dd>
        Data della richiesta, data prevista di rientro, stato e identità dei due utenti coinvolti.
        Finalità: gestire la richiesta e mostrarne lo stato a entrambe le parti.
      </dd>
    </dl>

    <h2 class="text-h6 mt-6 mb-2">3. Servizi esterni</h2>
    <p>Per alcune funzioni l'applicazione interroga servizi di terze parti:</p>
    <ul class="ms-6">
      <li>
        <strong>Nominatim / OpenStreetMap</strong>: conversione dell'indirizzo in coordinate e
        visualizzazione delle mappe.
      </li>
      <li>
        <strong>OpenLibrary</strong>: recupero dei metadati di un volume a partire dal codice ISBN.
      </li>
    </ul>
    <p class="mt-2">
      A questi servizi vengono inviati esclusivamente i dati necessari all'operazione richiesta
      (l'indirizzo da geocodificare, il codice ISBN da cercare), mai i dati dell'account.
    </p>

    <h2 class="text-h6 mt-6 mb-2">4. Conservazione</h2>
    <p>
      I dati restano memorizzati finché l'account esiste. Le statistiche giornaliere sono conservate
      in forma aggregata e non permettono di risalire ai singoli visitatori.
    </p>

    <h2 class="text-h6 mt-6 mb-2">5. Diritti dell'interessato</h2>
    <p>
      È possibile accedere ai propri dati, rettificarli dalla pagina di profilo e ottenerne la
      cancellazione eliminando l'account: l'eliminazione rimuove il profilo, le librerie pubblicate,
      i libri e le relative immagini. Nessun dato viene ceduto a terzi né usato per profilazione o
      finalità pubblicitarie.
    </p>

    <h2 class="text-h6 mt-6 mb-2">6. La tua scelta sul tracciamento</h2>
    <p class="mb-3">
      Stato attuale del consenso:
      <strong>{{ consentLabel }}</strong>
    </p>
    <div class="d-flex flex-wrap ga-2">
      <v-btn variant="outlined" color="primary" :disabled="status === 'granted'" @click="accept">
        Acconsento al conteggio delle visite
      </v-btn>
      <v-btn variant="outlined" :disabled="status === 'denied'" @click="reject">
        Nego il consenso
      </v-btn>
      <v-btn variant="text" :disabled="status === null" @click="revoke">
        Revoca e chiedimelo di nuovo
      </v-btn>
    </div>
  </v-container>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useConsent } from '@/composables/useConsent'

const { status, accept, reject, revoke } = useConsent()

const consentLabel = computed(() => {
  if (status.value === 'granted') return 'consenso prestato'
  if (status.value === 'denied') return 'consenso negato: nessun identificatore viene salvato'
  return 'nessuna scelta ancora effettuata'
})
</script>
