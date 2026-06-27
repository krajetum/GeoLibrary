<template>
  <v-app>
    <system-bar />
    <v-main>
      <router-view />
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import Search from '@/components/search.vue'
import SystemBar from '@/layouts/components/adminBar.vue'
</script>
