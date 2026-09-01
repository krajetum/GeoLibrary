<template>
  <!--
    Banner di consenso preventivo. Finché l'utente non sceglie, useApi non genera
    né invia l'identificatore pseudonimo. I due bottoni hanno la stessa evidenza
    visiva, perché negare il consenso deve costare quanto concederlo.
    Non è un modale: leggere l'informativa non deve richiedere di decidere prima.
  -->
  <v-sheet
    v-if="!hasDecided"
    class="cookie-banner pa-4"
    elevation="8"
    role="region"
    aria-labelledby="cookie-banner-title"
  >
    <v-container class="pa-0">
      <div class="d-flex flex-column flex-md-row align-md-center ga-4">
        <div>
          <h2 id="cookie-banner-title" class="text-subtitle-1 font-weight-medium mb-1">
            Rispettiamo i tuoi dati
          </h2>
          <p class="text-body-2 mb-0">
            Per contare quante volte una libreria o un libro vengono consultati salviamo sul tuo
            dispositivo un identificatore casuale, che non ti identifica personalmente e non viene
            condiviso con terzi. Puoi rifiutare: il sito continua a funzionare in ogni sua parte.
            <router-link to="/privacy">Informativa privacy</router-link>
          </p>
        </div>

        <v-spacer />

        <div class="d-flex ga-2 flex-shrink-0">
          <v-btn ref="rejectButton" variant="outlined" @click="reject">Rifiuta</v-btn>
          <v-btn variant="outlined" color="primary" @click="accept">Accetta</v-btn>
        </div>
      </div>
    </v-container>
  </v-sheet>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useConsent } from '@/composables/useConsent'

const { hasDecided, accept, reject } = useConsent()

const rejectButton = ref<{ $el: HTMLElement } | null>(null)
</script>

<style scoped>
/*
  Ancorato in basso e sopra al contenuto, ma senza scrim: la pagina resta
  navigabile e leggibile mentre si decide.
*/
.cookie-banner {
  position: fixed;
  inset-inline: 0;
  bottom: 0;
  z-index: 2000;
  border-top: 1px solid rgba(0, 0, 0, 0.12);
}
</style>
