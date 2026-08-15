<template>
  <v-container class="py-8">
    <h1 class="text-h4 mb-6">Importa libri da file</h1>

    <v-alert v-if="errorSummary" type="error" variant="tonal" class="mb-6" role="alert">
      {{ errorSummary }}
    </v-alert>

    <v-card variant="flat">
      <v-card-title>File ISBN</v-card-title>
      <v-card-text>
        <p class="text-body-2 text-medium-emphasis mb-4">
          Un ISBN per riga. I dati del libro (titolo, autore, ...) vengono recuperati
          automaticamente da un servizio esterno.
        </p>

        <!-- TODO: valutare v-file-upload (dropzone) come in book/new.vue -->
        <v-file-upload
          v-model="file"
          label="Seleziona file (.csv, .txt)"
          accept=".csv,.txt"
          variant="outlined"
          show-size
        />
      </v-card-text>
    </v-card>

    <div class="d-flex justify-end ga-3 mt-4">
      <v-btn variant="text" :disabled="importing" @click="onCancel"> Annulla </v-btn>
      <v-btn
        color="primary"
        :loading="importing"
        :disabled="!file"
        prepend-icon="mdi-upload"
        @click="onImport"
      >
        Importa
      </v-btn>
    </div>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" role="status">
      {{ snackbar.text }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useApi } from '@/composables/useApi'

const route = useRoute()
const router = useRouter()
const { apiFetch } = useApi()
const libraryId = route.params.id as string

const file = ref<File | undefined>(undefined)
const importing = ref(false)
const errorSummary = ref('')
const snackbar = reactive({ show: false, text: '', color: 'success' })

async function onImport() {
  if (!file.value) return

  errorSummary.value = ''
  importing.value = true
  try {
    const formData = new FormData()
    formData.append('file', file.value)

    const res = await apiFetch(`/library/${libraryId}/books/import`, {
      method: 'POST',
      body: formData,
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)

    const result = await res.json()
    snackbar.text = `Import completato: ${result.imported} aggiunti, ${result.skipped} scartati.`
    snackbar.color = 'success'
    snackbar.show = true
    router.push(`/app/libraries/${libraryId}`)
  } catch {
    errorSummary.value = "Si è verificato un errore durante l'import. Riprova."
    snackbar.text = 'Import non riuscito.'
    snackbar.color = 'error'
    snackbar.show = true
  } finally {
    importing.value = false
  }
}

function onCancel() {
  router.back()
}
</script>
