<template>
  <v-app-bar color="primary" dark>
    <v-app-bar-title>GeoLibrary</v-app-bar-title>
    <v-btn text to="/app" exact> Home </v-btn>
    <v-btn text to="/app/libraries" exact> My Libraries </v-btn>
    <v-btn text v-if="auth.hasRole('admin')" exact>Administration Panel</v-btn>
    <v-spacer />
    <v-menu min-width="200px" :close-on-content-click="false">
      <template v-slot:activator="{ props }">
        <v-btn icon v-bind="props">
          <v-avatar color="brown" size="large">
            <span class="text-headline-small">{{ user.initials }}</span>
          </v-avatar>
        </v-btn>
      </template>
      <v-card>
        <v-card-text>
          <div class="mx-auto text-center">
            <v-avatar color="brown">
              <span class="text-headline-small">{{ user.initials }}</span>
            </v-avatar>
            <h3 class="my-0">{{ user.fullName }}</h3>
            <p class="text-body-small mt-1">
              {{ user.email }}
            </p>
            <v-divider class="my-3"></v-divider>
            <v-btn variant="text" rounded> Edit Account </v-btn>
            <v-divider class="my-3"></v-divider>
            <v-btn variant="text" rounded @click="auth.logout()"> Disconnect </v-btn>
          </div>
        </v-card-text>
      </v-card>
    </v-menu>
  </v-app-bar>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()

const user = computed(() => {
  const info = auth.userInfo
  const fullName = info.displayName ?? info.username ?? ''
  const initials = fullName
    .split(' ')
    .map((part) => part.charAt(0))
    .join('')
    .slice(0, 2)
    .toUpperCase()

  return {
    fullName,
    email: info.email ?? '',
    initials,
  }
})
</script>
