<template>
  <v-main>
    <!-- Search Bar -->
    <v-container>
      <search />
    </v-container>

    <!-- Welcome Banner -->
    <v-sheet color="primary" class="text-center py-10">
      <h1 class="text-h3 font-weight-bold text-white">Welcome to GeoLibrary</h1>
      <p class="text-h6 mt-2 text-white">Your geospatial data management solution</p>
    </v-sheet>

    <v-container class="py-8">
      <h2 class="text-h5 font-weight-bold text-center mb-4">About GeoLibrary</h2>
      <p class="text-center text-medium-emphasis">
        GeoLibrary is a solution for sharing the books of private libraries.
      </p>
    </v-container>
  </v-main>
</template>

<script setup lang="ts">
import Search from '@/components/search.vue'
</script>
