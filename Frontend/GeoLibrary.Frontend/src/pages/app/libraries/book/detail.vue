<template>
  <v-container class="py-8">
    <h1 class="text-h4 mb-6">Dettagli libro</h1>

    <v-card v-if="book" class="mb-6">
      <v-card-title>{{ book.title }}</v-card-title>
      <v-card-text>
        <p><strong>Descrizione:</strong> {{ book.description }}</p>
        <p><strong>ISBN:</strong> {{ book.isbn }}</p>
      </v-card-text>
    </v-card>

    <v-alert v-else type="error" variant="tonal" role="alert">
      Impossibile caricare i dettagli del libro.
    </v-alert>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useApi } from '@/composables/useApi'
import { useRoute } from 'vue-router'

const route = useRoute()

const api = useApi()
const book = ref<any>(null)

onMounted(async () => {
  const libraryId = route.params.id as string
  const bookId = route.params.bookId as string

  console.log('Fetching book details for libraryId:', libraryId, 'bookId:', bookId)
  const response = await api.apiFetch(`/library/${libraryId}/book/${bookId}`)
  if (response.status === 200) {
    console.log('Book details fetched successfully')
    book.value = await response.json()
  } else {
    console.error('Failed to fetch book details')
  }
})
</script>
