<template>
  <v-responsive>
    <v-app>
      <v-app-bar>
        <v-spacer></v-spacer>
        <template v-if="auth.isAuthenticated">
          <span class="mr-4">{{ auth.username }}</span>
          <v-btn to="/app/">Vai all'app</v-btn>
          <v-btn @click="auth.logout()">Logout</v-btn>
        </template>
        <template v-else>
          <v-btn @click="auth.login('/app/')">Login</v-btn>
          <v-btn @click="auth.register('/app/')">Registrati</v-btn>
        </template>
      </v-app-bar>
      <v-main>
        <v-sheet color="primary" class="text-center py-10">
          <h1 class="text-h3 font-weight-bold text-white">Welcome to GeoLibrary</h1>
        </v-sheet>

        <v-container class="py-8">
          <h2 class="text-h5 font-weight-bold text-center mb-4">About GeoLibrary</h2>
          <p class="text-center text-medium-emphasis">
            GeoLibrary is a solution for sharing the books of private libraries.
          </p>
          <p class="text-center text-medium-emphasis">
            Share your books to other registered people, spread culture!
          </p>
        </v-container>
      </v-main>
    </v-app>
  </v-responsive>
</template>

<script setup lang="ts">
// import Search from '@/components/search.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
</script>
