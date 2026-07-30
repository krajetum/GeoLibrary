<template>
  <v-container class="py-8">
    <h1 class="text-h4 mb-6">Modifica libreria</h1>

    <v-skeleton-loader v-if="loading" type="article" />
    <!-- Il form legge i dati una volta sola all'avvio, quindi si monta solo quando sono pronti. -->
    <library-form
      v-else-if="library"
      :library="library"
      :saving="saving"
      :error="errorSummary"
      submit-label="Salva modifiche"
      @submit="onSubmit"
      @cancel="router.back()"
    />
    <v-alert v-else type="error" variant="tonal" role="alert">
      Impossibile caricare la libreria.
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
import LibraryForm from '@/components/library-form.vue'

const route = useRoute()
const router = useRouter()
const { apiFetch } = useApi()

const id = route.params.id as string

const library = ref<any>(null)
const loading = ref(true)
const saving = ref(false)
const errorSummary = ref('')
const snackbar = reactive({ show: false, text: '', color: 'success' })

onMounted(async () => {
  const res = await apiFetch(`/library/${id}`)
  if (res.ok) {
    library.value = await res.json()
  }
  loading.value = false
})

async function onSubmit(payload: any, image?: File) {
  errorSummary.value = ''
  saving.value = true
  try {
    const res = await apiFetch(`/library/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)

    // L'immagine si carica solo se ne è stata scelta una nuova, altrimenti resta quella salvata.
    if (image) {
      const body = new FormData()
      body.append('file', image)

      const imageRes = await apiFetch(`/library/${id}/image`, { method: 'POST', body })
      if (!imageRes.ok) throw new Error(`HTTP ${imageRes.status}`)
    }

    router.push(`/app/libraries/${id}`)
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
