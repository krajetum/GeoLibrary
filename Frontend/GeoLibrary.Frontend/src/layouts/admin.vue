<template>
  <v-app>
    <system-bar />
    <v-main>
      <breadcrumbs />
      <router-view />
    </v-main>
  </v-app>
</template>

<script setup lang="ts">
import Breadcrumbs from '@/components/breadcrumbs.vue'
import SystemBar from '@/layouts/components/adminBar.vue'
</script>
