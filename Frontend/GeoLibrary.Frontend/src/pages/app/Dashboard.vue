<template>
  <v-row>
    <v-col cols="12">
      <Libraries :libraries="libraries" />
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useApi } from '@/composables/useApi'
import Libraries from '@/components/libraries.vue'

var backend = useApi()

const libraries = ref<any[]>([])

onMounted(async () => {
  var response = await backend.apiFetch('/library')
  if (response.status !== 200) {
    // TODO: Handle error
  }

  libraries.value = await response.json()
})
</script>
