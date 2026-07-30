<template>
  <v-container class="py-8">
    <h1 class="text-h4 mb-6">Modifica libro</h1>

    <v-skeleton-loader v-if="loading" type="article" />
    <!-- Il form legge i dati una volta sola all'avvio, quindi si monta solo quando sono pronti. -->
    <book-form
      v-else-if="book"
      :book="book"
      :saving="saving"
      :error="errorSummary"
      submit-label="Salva modifiche"
      @submit="onSubmit"
      @cancel="router.back()"
    />
    <v-alert v-else type="error" variant="tonal" role="alert">
      Impossibile caricare il libro.
    </v-alert>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" role="status">
      {{ snackbar.text }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useApi } from '@/composables/useApi'
import BookForm from '@/components/book-form.vue'

const route = useRoute()
const router = useRouter()
const { apiFetch } = useApi()

const libraryId = route.params.id as string
const bookId = route.params.bookId as string

const book = ref<any>(null)
const loading = ref(true)
const saving = ref(false)
const errorSummary = ref('')
const snackbar = reactive({ show: false, text: '', color: 'success' })

onMounted(async () => {
  const res = await apiFetch(`/library/${libraryId}/books/${bookId}`)
  if (res.ok) {
    book.value = await res.json()
  }
  loading.value = false
})

async function onSubmit(payload: any, image?: File) {
  errorSummary.value = ''
  saving.value = true
  try {
    // libraryId è richiesto dal validator anche in modifica, ma il server lo ignora.
    const res = await apiFetch(`/book/${bookId}`, {
      method: 'PUT',
      body: JSON.stringify({ libraryId, ...payload }),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)

    // La copertina si carica solo se ne è stata scelta una nuova.
    if (image) {
      const body = new FormData()
      body.append('file', image)

      const imageRes = await apiFetch(`/book/${bookId}/cover`, { method: 'POST', body })
      if (!imageRes.ok) throw new Error(`HTTP ${imageRes.status}`)
    }

    router.push(`/app/libraries/${libraryId}/book/${bookId}`)
  } catch {
    errorSummary.value = 'Si è verificato un errore durante il salvataggio. Riprova.'
    snackbar.text = 'Salvataggio non riuscito.'
    snackbar.color = 'error'
    snackbar.show = true
  } finally {
    saving.value = false
  }
}
</script>
