<template>
  <!--
    Footer minimo, presente in entrambi i layout
    <v-footer app> lo ancora in fondo alla pagina anche quando il contenuto
    è più corto della finestra.
  -->
  <v-footer app class="border-t-thin bg-surface">
    <v-container class="pa-0 d-flex flex-wrap align-center ga-2">
      <span class="text-caption text-medium-emphasis">
        GeoLibrary &middot; prototipo accademico
      </span>
      <v-spacer />
      <nav aria-label="Link di servizio">
        <router-link to="/privacy" class="text-caption">Informativa privacy</router-link>
      </nav>
    </v-container>
  </v-footer>
</template>
