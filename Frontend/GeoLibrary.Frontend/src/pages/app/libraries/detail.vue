<template>
  <v-container>
    <v-row>
      <v-col>
        <v-card variant="flat" class="rounded-lg overflow-hidden">
          <v-img :src="library.imageUrl ?? '/placeholder_library.jpg'" cover height="260">
            <div class="hero-overlay d-flex flex-column justify-end fill-height pa-4">
              <div class="text-white">
                <h1 class="text-h4 font-weight-bold text-truncate">{{ library.name }}</h1>
                <div class="d-flex align-center mt-1">
                  <v-icon size="18" class="me-1">mdi-map-marker</v-icon>
                  <span class="text-body-2 text-truncate">{{ position }}</span>
                </div>
              </div>
            </div>
          </v-img>

          <div class="d-flex flex-wrap align-center ga-3 px-4 py-3 border-t-thin">
            <div class="d-flex flex-wrap ga-2">
              <v-chip size="small" variant="tonal" color="primary" prepend-icon="mdi-book-multiple">
                {{ library.bookCount ?? 0 }} libri
              </v-chip>
              <v-chip size="small" variant="tonal" prepend-icon="mdi-eye-outline">
                {{ library.viewsCount ?? 0 }} visite
              </v-chip>
              <v-chip
                v-if="library.isHidden"
                size="small"
                variant="tonal"
                prepend-icon="mdi-eye-off-outline"
              >
                Nascosta
              </v-chip>
              <v-chip
                v-if="library.isApproximateLocation"
                size="small"
                variant="tonal"
                prepend-icon="mdi-map-marker-radius"
              >
                Posizione approssimativa
              </v-chip>
            </div>

            <v-spacer />

            <div class="d-flex align-center ga-2">
              <!-- Azioni di modifica: solo per il proprietario -->
              <template v-if="library.isAdmin">
                <v-btn color="primary" prepend-icon="mdi-pencil" @click="onEdit"> Modifica </v-btn>
                <v-btn variant="tonal" prepend-icon="mdi-book-plus" @click="onAddBook">
                  Aggiungi libro
                </v-btn>
                <v-btn prepend-icon="mdi-file-upload-outline" @click="onImportCSV"
                  >Importa CSV</v-btn
                >
              </template>

              <v-menu location="bottom end">
                <template #activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon="mdi-dots-vertical"
                    variant="text"
                    aria-label="Altre azioni"
                  />
                </template>
                <v-list density="comfortable" min-width="200">
                  <v-list-item
                    prepend-icon="mdi-share-variant"
                    title="Condividi"
                    @click="onShare"
                  />
                  <v-list-item
                    prepend-icon="mdi-google-maps"
                    title="Apri su Google Maps"
                    @click="goToMaps"
                  />
                  <template v-if="library.isAdmin">
                    <v-divider class="my-1" />
                    <v-list-item
                      prepend-icon="mdi-delete"
                      title="Elimina"
                      base-color="error"
                      @click="confirmDelete = true"
                    />
                  </template>
                </v-list>
              </v-menu>
            </div>
          </div>
        </v-card>
      </v-col>
    </v-row>

    <v-row>
      <v-col>
        <books-table :library-id="id" />
      </v-col>
    </v-row>

    <!-- Conferma eliminazione -->
    <v-dialog v-model="confirmDelete" max-width="420">
      <v-card>
        <v-card-title>Elimina libreria</v-card-title>
        <v-card-text>
          Questa operazione è irreversibile. Vuoi davvero eliminare «{{ library.name }}»?
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" @click="confirmDelete = false">Annulla</v-btn>
          <v-btn color="error" :loading="deleting" @click="onDelete">Elimina</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" role="status">
      {{ snackbar.text }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useApi } from '@/composables/useApi'
import BooksTable from '@/components/books-table.vue'

const api = useApi()
const route = useRoute()
const router = useRouter()

const library = ref<any>({})

const confirmDelete = ref(false)
const deleting = ref(false)
const snackbar = reactive({ show: false, text: '', color: 'success' })

const id = route.params.id as string

// Il server manda l'indirizzo solo a chi ne ha diritto: quando manca resta la città.
const position = computed(() => {
  const l = library.value
  return l.address ?? [l.city, l.countryCode].filter(Boolean).join(', ')
})

function notify(text: string, color: 'success' | 'error' = 'success') {
  snackbar.text = text
  snackbar.color = color
  snackbar.show = true
}

onMounted(async () => {
  const response = await api.apiFetch(`/library/${id}`)
  if (response.status !== 200) {
    notify('Impossibile caricare la libreria.', 'error')
  } else {
    library.value = await response.json()
  }
})

function onEdit() {
  router.push(`/app/libraries/${id}/edit`)
}

function onAddBook() {
  router.push(`/app/libraries/${id}/book/new`)
}

function onImportCSV() {
  router.push(`/app/libraries/${id}/book/import`)
}

function onShare() {
  navigator.clipboard?.writeText(window.location.href)
  notify('Link copiato negli appunti.')
}

/*
Source - https://stackoverflow.com/a/6240537
Posted by Yilmaz Guleryuz, modified by community. See post 'Timeline' for change history
Retrieved 2026-06-29, License - CC BY-SA 3.0
*/
function goToMaps() {
  window.open(
    `https://maps.google.com/?q=${library.value.latitude},${library.value.longitude}`,
    '_blank',
  )
}

async function onDelete() {
  deleting.value = true
  try {
    const res = await api.apiFetch(`/library/${id}`, { method: 'DELETE' })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    notify('Libreria eliminata.')
    router.push('/app/libraries')
  } catch {
    notify('Eliminazione non riuscita.', 'error')
  } finally {
    deleting.value = false
    confirmDelete.value = false
  }
}
</script>

<style scoped>
/* Scrim scuro solo in basso per rendere leggibile il titolo sull'immagine */
.hero-overlay {
  background: linear-gradient(
    to top,
    rgba(0, 0, 0, 0.7) 0%,
    rgba(0, 0, 0, 0.2) 35%,
    transparent 60%
  );
}
</style>
