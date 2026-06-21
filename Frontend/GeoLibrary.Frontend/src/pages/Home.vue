<template>
  <div class="h-screen">
    <!-- Search Bar -->
    <search />

    <!-- Welcome Banner -->
    <div class="bg-blue-500 text-white text-center py-10">
      <h1 class="text-4xl font-bold">Welcome to GeoLibrary</h1>
      <p class="text-lg mt-2">Your geospatial data management solution</p>
    </div>

    <div class="container mx-auto px-4 py-8">
      <h2 class="text-2xl font-bold text-center mb-4">About GeoLibrary</h2>
      <p class="text-center text-gray-600">
        GeoLibrary is a solution for sharing the books of private libraries.
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import Search from '@/components/search.vue'
</script>
