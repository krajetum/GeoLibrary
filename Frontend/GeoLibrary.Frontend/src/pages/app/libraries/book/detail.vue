<template>
  <v-container class="py-8">
    <v-skeleton-loader v-if="loading" type="card" />

    <template v-else-if="book">
      <v-card variant="flat" class="rounded-lg overflow-hidden mb-6">
        <v-row no-gutters>
          <v-col cols="12" sm="4" md="3" class="pa-4">
            <v-img
              v-if="book.coverImageUrl"
              :src="book.coverImageUrl"
              :alt="`Copertina di ${book.title}`"
              :aspect-ratio="2 / 3"
              max-width="220"
              cover
              class="mx-auto rounded-lg"
            />
            <!-- Stesso ripiego della tabella libri quando manca la copertina -->
            <v-responsive
              v-else
              :aspect-ratio="2 / 3"
              max-width="220"
              class="mx-auto rounded-lg bg-surface-light d-flex align-center justify-center"
            >
              <v-icon icon="mdi-book-outline" size="48" class="text-medium-emphasis" />
            </v-responsive>
          </v-col>

          <v-col cols="12" sm="8" md="9" class="pa-4">
            <h1 class="text-h4 font-weight-bold">{{ book.title }}</h1>

            <div class="d-flex align-center mt-1 text-medium-emphasis">
              <v-icon size="18" class="me-1">mdi-account-edit-outline</v-icon>
              <span class="text-body-1">{{ book.author }}</span>
            </div>

            <div class="d-flex flex-wrap ga-2 mt-4">
              <v-chip size="small" variant="tonal" color="primary" prepend-icon="mdi-book-multiple">
                {{ book.totalCopies }} copie
              </v-chip>
              <v-chip v-if="book.isbn" size="small" variant="tonal" prepend-icon="mdi-barcode">
                {{ book.isbn }}
              </v-chip>
              <v-chip
                v-if="book.isHidden"
                size="small"
                variant="tonal"
                prepend-icon="mdi-eye-off-outline"
              >
                Nascosto
              </v-chip>
            </div>

            <!-- TODO: manca l'endpoint dei prestiti, per ora il bottone non fa nulla -->
            <v-btn
              v-if="!book.isAdmin"
              color="primary"
              prepend-icon="mdi-bookmark-plus-outline"
              class="mt-6"
            >
              Richiedi in prestito
            </v-btn>
          </v-col>
        </v-row>

        <div class="d-flex flex-wrap align-center ga-3 px-4 py-3 border-t-thin">
          <v-spacer />
          <v-btn
            v-if="book.isAdmin"
            color="primary"
            prepend-icon="mdi-pencil"
            :to="`/app/libraries/${libraryId}/book/${bookId}/edit`"
          >
            Modifica
          </v-btn>
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
              <v-list-item prepend-icon="mdi-share-variant" title="Condividi" @click="onShare" />
              <template v-if="book.isAdmin">
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
      </v-card>

      <v-card variant="flat" class="rounded-lg mb-6">
        <v-card-title class="text-h6">Descrizione</v-card-title>
        <v-card-text>
          <p v-if="book.description" class="text-body-1 descrizione">{{ book.description }}</p>
          <span v-else class="text-medium-emphasis">Nessuna descrizione disponibile.</span>
        </v-card-text>
      </v-card>

      <!-- Statistiche e richieste: solo per chi possiede la libreria -->
      <template v-if="book.isAdmin">
        <v-card variant="flat" class="rounded-lg mb-6">
          <v-card-title class="text-h6">Statistiche</v-card-title>
          <v-card-text>
            <v-row>
              <v-col cols="6" md="3">
                <div class="text-h4">{{ stats.viewsCount }}</div>
                <div class="text-caption text-medium-emphasis">Visualizzazioni</div>
              </v-col>
              <v-col cols="6" md="3">
                <div class="text-h4">{{ stats.pendingCount }}</div>
                <div class="text-caption text-medium-emphasis">Richieste in attesa</div>
              </v-col>
              <v-col cols="6" md="3">
                <div class="text-h4">{{ stats.activeLoansCount }}</div>
                <div class="text-caption text-medium-emphasis">Prestiti attivi</div>
              </v-col>
              <v-col cols="6" md="3">
                <div class="text-h4">{{ book.totalCopies }}</div>
                <div class="text-caption text-medium-emphasis">Copie totali</div>
              </v-col>
            </v-row>

            <div class="text-caption text-medium-emphasis mt-6 mb-2">
              Visualizzazioni degli ultimi 7 giorni
            </div>
            <v-sparkline
              :model-value="stats.dailyViews"
              :min="0"
              color="primary"
              height="80"
              line-width="2"
              padding="12"
              smooth
              auto-draw
            />
          </v-card-text>
        </v-card>

        <v-card variant="flat" class="rounded-lg">
          <v-card-title class="text-h6">Richieste di prestito</v-card-title>
          <v-list lines="two">
            <v-list-item
              v-for="loan in stats.loans"
              :key="loan.id"
              :title="loan.userDisplayName"
              :subtitle="`Richiesto il ${loan.bookingDate} · rientro entro il ${loan.returnDate}`"
            >
              <template #prepend>
                <v-avatar color="surface-light">
                  <v-icon icon="mdi-account" />
                </v-avatar>
              </template>
              <template #append>
                <v-chip
                  size="small"
                  variant="tonal"
                  :color="statusColor(loan.status)"
                  :prepend-icon="statusIcon(loan.status)"
                >
                  {{ statusLabel(loan.status) }}
                </v-chip>
              </template>
            </v-list-item>
          </v-list>
        </v-card>
      </template>
    </template>

    <v-alert v-else type="error" variant="tonal" role="alert">
      Impossibile caricare i dettagli del libro.
    </v-alert>

    <!-- Conferma eliminazione -->
    <v-dialog v-model="confirmDelete" max-width="420">
      <v-card>
        <v-card-title>Elimina libro</v-card-title>
        <v-card-text>
          Questa operazione è irreversibile. Vuoi davvero eliminare «{{ book?.title }}»?
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
import { reactive, ref, onMounted } from 'vue'
import { useApi } from '@/composables/useApi'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const api = useApi()
const book = ref<any>(null)
const loading = ref(true)

const libraryId = route.params.id as string
const bookId = route.params.bookId as string

const confirmDelete = ref(false)
const deleting = ref(false)
const snackbar = reactive({ show: false, text: '', color: 'success' })

// TODO: dati finti finché non esistono gli endpoint di viste e prestiti.
const stats = {
  viewsCount: 128,
  pendingCount: 2,
  activeLoansCount: 1,
  dailyViews: [4, 9, 6, 14, 8, 11, 7],
  loans: [
    {
      id: '1',
      userDisplayName: 'Mario Rossi',
      bookingDate: '12/07/2026',
      returnDate: '11/08/2026',
      status: 'Approved',
    },
    {
      id: '2',
      userDisplayName: 'Giulia Bianchi',
      bookingDate: '28/07/2026',
      returnDate: '27/08/2026',
      status: 'Pending',
    },
    {
      id: '3',
      userDisplayName: 'Luca Verdi',
      bookingDate: '29/07/2026',
      returnDate: '28/08/2026',
      status: 'Pending',
    },
  ],
}

function statusLabel(status: string) {
  return {
    Pending: 'In attesa',
    Approved: 'Attivo',
    Rejected: 'Rifiutato',
    Returned: 'Restituito',
  }[status]
}

function statusColor(status: string) {
  return { Pending: 'warning', Approved: 'success', Rejected: 'error', Returned: '' }[status]
}

function statusIcon(status: string) {
  return {
    Pending: 'mdi-clock-outline',
    Approved: 'mdi-check',
    Rejected: 'mdi-close',
    Returned: 'mdi-keyboard-return',
  }[status]
}

function notify(text: string, color: 'success' | 'error' = 'success') {
  snackbar.text = text
  snackbar.color = color
  snackbar.show = true
}

onMounted(async () => {
  const response = await api.apiFetch(`/library/${libraryId}/books/${bookId}`)
  if (response.status === 200) {
    book.value = await response.json()
  }
  loading.value = false
})

function onShare() {
  navigator.clipboard?.writeText(window.location.href)
  notify('Link copiato negli appunti.')
}

async function onDelete() {
  deleting.value = true
  try {
    const res = await api.apiFetch(`/book/${bookId}`, { method: 'DELETE' })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    router.push(`/app/libraries/${libraryId}`)
  } catch {
    notify('Eliminazione non riuscita.', 'error')
  } finally {
    deleting.value = false
    confirmDelete.value = false
  }
}
</script>

<style scoped>
/* Gli a capo scritti nella descrizione altrimenti si perderebbero */
.descrizione {
  white-space: pre-line;
}
</style>
