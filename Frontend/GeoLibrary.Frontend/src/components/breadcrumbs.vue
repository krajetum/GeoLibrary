<template>
  <!-- Sulla dashboard resta la sola voce Home: in quel caso non si mostra nulla -->
  <v-breadcrumbs v-if="items.length > 1" :items="items" density="comfortable" />
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useApi } from '@/composables/useApi'

const route = useRoute()
const { apiFetch } = useApi()

// I nomi se li recupera il componente: le pagine di creazione e import non
// caricano la libreria, quindi non potrebbero passarli loro.
const libraryName = ref('')
const bookTitle = ref('')

watch(
  () => route.params.id as string | undefined,
  async (id) => {
    libraryName.value = ''
    if (!id) return

    const res = await apiFetch(`/library/${id}`)
    // Si controlla di essere ancora sulla stessa libreria: una navigazione veloce
    // può far arrivare la risposta vecchia dopo quella nuova.
    if (res.ok && route.params.id === id) {
      libraryName.value = (await res.json()).name
    }
  },
  { immediate: true },
)

watch(
  () => route.params.bookId as string | undefined,
  async (bookId) => {
    bookTitle.value = ''
    if (!bookId) return

    const res = await apiFetch(`/library/${route.params.id}/books/${bookId}`)
    if (res.ok && route.params.bookId === bookId) {
      bookTitle.value = (await res.json()).title
    }
  },
  { immediate: true },
)

const items = computed(() => {
  const trail: { title: string; to?: string; disabled?: boolean }[] = [
    { title: 'Home', to: '/app/' },
  ]

  if (route.path.startsWith('/app/libraries')) {
    trail.push({ title: 'Biblioteche', to: '/app/libraries' })
  }

  const libraryId = route.params.id as string | undefined
  if (libraryId) {
    trail.push({ title: libraryName.value || '…', to: `/app/libraries/${libraryId}` })
  }

  const bookId = route.params.bookId as string | undefined
  if (bookId) {
    trail.push({
      title: bookTitle.value || '…',
      to: `/app/libraries/${libraryId}/book/${bookId}`,
    })
  }

  // Pagine di azione: il titolo è fisso e sta sulla route
  const title = route.meta.title as string | undefined
  if (title) {
    trail.push({ title })
  }

  // L'ultima voce è la pagina corrente, quindi non è un link
  const current = trail.at(-1)
  if (current) {
    current.disabled = true
  }

  return trail
})
</script>
