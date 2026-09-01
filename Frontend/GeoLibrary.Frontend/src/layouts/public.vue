<template>
  <v-app>
    <v-app-bar color="primary">
      <v-app-bar-title>
        <router-link to="/" class="text-white text-decoration-none">GeoLibrary</router-link>
      </v-app-bar-title>
      <v-spacer />
      <template v-if="auth.isAuthenticated">
        <span class="mr-4 d-none d-sm-inline">{{ auth.username }}</span>
        <v-btn to="/app/">Vai all'app</v-btn>
        <v-btn @click="auth.logout()">Esci</v-btn>
      </template>
      <template v-else>
        <v-btn @click="auth.login(route.fullPath)">Accedi</v-btn>
        <v-btn @click="auth.register(route.fullPath)">Registrati</v-btn>
      </template>
    </v-app-bar>
    <v-main>
      <breadcrumbs />
      <router-view />
    </v-main>
    <site-footer />
    <cookie-banner />
  </v-app>
</template>

<script setup lang="ts">
import { useRoute } from 'vue-router'
import Breadcrumbs from '@/components/breadcrumbs.vue'
import CookieBanner from '@/components/cookie-banner.vue'
import SiteFooter from '@/layouts/components/siteFooter.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const route = useRoute()
</script>
