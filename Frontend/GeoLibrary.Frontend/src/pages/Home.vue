<template>
  <v-sheet color="primary" class="text-center py-10">
    <h1 class="text-h3 font-weight-bold text-white">Benvenuto su GeoLibrary</h1>
    <p class="text-white mt-2">
      Condividi i libri della tua biblioteca privata e trova quelli vicino a te.
    </p>
  </v-sheet>

  <v-container class="py-8">
    <book-area-search />
  </v-container>
</template>

<script setup lang="ts">
import BookAreaSearch from '@/components/book-area-search.vue'
</script>
