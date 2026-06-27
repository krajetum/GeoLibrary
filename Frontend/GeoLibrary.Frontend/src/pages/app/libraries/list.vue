<template>
  <v-row>
    <v-col cols="12">
      <Libraries :libraries="libraries" />
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
import Libraries from '@/components/libraries.vue'

const libraries = [
  {
    id: 'testId',
    title: 'My first library',
    img: '/placeholder_library.jpg',
  },
]
</script>
