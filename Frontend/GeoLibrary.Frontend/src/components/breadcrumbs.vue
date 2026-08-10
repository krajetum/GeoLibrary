<template>
  <!-- Sulla dashboard resta la sola voce Home: in quel caso non si mostra nulla -->
  <v-breadcrumbs v-if="items.length > 1" :items="items" density="comfortable" />
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useApi } from '@/composables/useApi'
import { useAppLink } from '@/composables/useAppLink'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const { apiFetch } = useApi()
const { libraryPath, bookPath } = useAppLink()
const auth = useAuthStore()

// I nomi se li recupera il componente: le pagine di creazione e import non
// caricano la libreria, quindi non potrebbero passarli loro.
const libraryName = ref('')
const bookTitle = ref('')
// Parte da false così la voce "Biblioteche" compare a conferma avvenuta,
// invece di apparire e sparire mentre la risposta è in volo.
const libraryIsMine = ref(false)

watch(
  () => route.params.id as string | undefined,
  async (id) => {
    libraryName.value = ''
    libraryIsMine.value = false
    if (!id) return

    const res = await apiFetch(`/library/${id}`)
    // Si controlla di essere ancora sulla stessa libreria: una navigazione veloce
    // può far arrivare la risposta vecchia dopo quella nuova.
    if (res.ok && route.params.id === id) {
      const library = await res.json()
      libraryName.value = library.name
      libraryIsMine.value = library.isAdmin
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
    { title: 'Home', to: auth.isAuthenticated ? '/app/' : '/' },
  ]

  const libraryId = route.params.id as string | undefined

  // "Biblioteche" porta all'elenco delle proprie librerie: ha senso solo mentre si
  // naviga il proprio patrimonio, non quando si sta guardando quello di qualcun altro.
  if (route.path.startsWith('/app/libraries') && (!libraryId || libraryIsMine.value)) {
    trail.push({ title: 'Biblioteche', to: '/app/libraries' })
  }

  if (libraryId) {
    trail.push({ title: libraryName.value || '…', to: libraryPath(libraryId) })
  }

  const bookId = route.params.bookId as string | undefined
  if (bookId && libraryId) {
    trail.push({
      title: bookTitle.value || '…',
      to: bookPath(libraryId, bookId),
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
