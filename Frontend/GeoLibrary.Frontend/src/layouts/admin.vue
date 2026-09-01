<template>
  <v-app>
    <system-bar />
    <v-main>
      <breadcrumbs />
      <router-view />
    </v-main>
    <site-footer />
    <cookie-banner />
  </v-app>
</template>

<script setup lang="ts">
import Breadcrumbs from '@/components/breadcrumbs.vue'
import CookieBanner from '@/components/cookie-banner.vue'
import SiteFooter from '@/layouts/components/siteFooter.vue'
import SystemBar from '@/layouts/components/adminBar.vue'
</script>
