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

            <div v-if="!book.isAdmin" class="mt-6">
              <v-btn
                v-if="!auth.isAuthenticated"
                color="primary"
                prepend-icon="mdi-login"
                @click="auth.login(route.fullPath)"
              >
                Accedi per richiedere il prestito
              </v-btn>

              <v-chip
                v-else-if="myLoan"
                size="large"
                variant="tonal"
                :color="statusColor(myLoan.status)"
                :prepend-icon="statusIcon(myLoan.status)"
              >
                {{ myLoan.status === 'Approved' ? 'Prestito attivo' : 'Richiesta in attesa' }}
              </v-chip>

              <v-btn
                v-else
                color="primary"
                prepend-icon="mdi-bookmark-plus-outline"
                @click="openRequestDialog"
              >
                Richiedi in prestito
              </v-btn>
            </div>
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

      <!-- Il server manda l'indirizzo completo solo a chi ne ha diritto: qui si mostra
           quello che è arrivato, senza decidere niente lato client. -->
      <v-card v-if="!book.isAdmin && library" variant="flat" class="rounded-lg mb-6">
        <v-card-title class="text-h6">Dove ritirare il libro</v-card-title>
        <v-card-text>
          <div class="d-flex align-center">
            <v-icon size="18" class="me-1">mdi-map-marker</v-icon>
            <span class="text-body-1">{{ pickupPosition }}</span>
          </div>
          <p v-if="library.isApproximateLocation" class="text-caption text-medium-emphasis mt-2">
            L'indirizzo esatto viene mostrato quando il proprietario accetta la richiesta di
            prestito.
          </p>
        </v-card-text>
      </v-card>

      <!-- Statistiche e richieste: solo per chi possiede la libreria -->
      <template v-if="book.isAdmin">
        <v-card variant="flat" class="rounded-lg mb-6">
          <v-card-title class="text-h6">Statistiche</v-card-title>
          <v-card-text>
            <!-- TODO: le visualizzazioni non hanno ancora un endpoint che le conti,
                 quindi qui non compaiono. -->
            <v-row>
              <v-col cols="6" md="4">
                <div class="text-h4">{{ pendingCount }}</div>
                <div class="text-caption text-medium-emphasis">Richieste in attesa</div>
              </v-col>
              <v-col cols="6" md="4">
                <div class="text-h4">{{ activeLoansCount }}</div>
                <div class="text-caption text-medium-emphasis">Prestiti attivi</div>
              </v-col>
              <v-col cols="6" md="4">
                <div class="text-h4">{{ book.totalCopies }}</div>
                <div class="text-caption text-medium-emphasis">Copie totali</div>
              </v-col>
            </v-row>
          </v-card-text>
        </v-card>

        <v-card variant="flat" class="rounded-lg">
          <v-card-title class="text-h6">Richieste di prestito</v-card-title>
          <v-list lines="two">
            <v-list-item
              v-for="loan in loans"
              :key="loan.id"
              :title="loan.userDisplayName"
              :subtitle="`Richiesto il ${formatDate(loan.bookingDate)} · rientro entro il ${formatDate(loan.returnDate)}`"
            >
              <template #prepend>
                <v-avatar color="surface-light">
                  <v-icon icon="mdi-account" />
                </v-avatar>
              </template>
              <template #append>
                <div class="d-flex align-center ga-2">
                  <v-chip
                    size="small"
                    variant="tonal"
                    :color="statusColor(loan.status)"
                    :prepend-icon="statusIcon(loan.status)"
                  >
                    {{ statusLabel(loan.status) }}
                  </v-chip>

                  <template v-if="loan.status === 'Pending'">
                    <v-btn
                      size="small"
                      variant="tonal"
                      color="success"
                      :loading="updatingId === loan.id"
                      @click="updateStatus(loan, 'Approved')"
                    >
                      Approva
                    </v-btn>
                    <v-btn
                      size="small"
                      variant="text"
                      :loading="updatingId === loan.id"
                      @click="updateStatus(loan, 'Rejected')"
                    >
                      Rifiuta
                    </v-btn>
                  </template>

                  <v-btn
                    v-else-if="loan.status === 'Approved'"
                    size="small"
                    variant="tonal"
                    :loading="updatingId === loan.id"
                    @click="updateStatus(loan, 'Returned')"
                  >
                    Segna restituito
                  </v-btn>
                </div>
              </template>
            </v-list-item>

            <v-list-item v-if="!loans.length">
              <span class="text-medium-emphasis">Nessuna richiesta ricevuta.</span>
            </v-list-item>
          </v-list>
        </v-card>
      </template>
    </template>

    <v-alert v-else type="error" variant="tonal" role="alert">
      Impossibile caricare i dettagli del libro.
    </v-alert>

    <!-- Richiesta di prestito -->
    <v-dialog v-model="requestDialog" max-width="420">
      <v-card>
        <v-card-title>Richiedi in prestito</v-card-title>
        <v-card-text>
          <p class="text-body-2 text-medium-emphasis mb-4">
            Il proprietario riceverà la richiesta insieme al tuo nome e potrà accettarla o
            rifiutarla.
          </p>
          <v-text-field
            v-model="returnDate"
            type="date"
            label="Data di rientro"
            :min="minReturnDate"
            :max="maxReturnDate"
            variant="outlined"
            :error-messages="requestError"
          />
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" @click="requestDialog = false">Annulla</v-btn>
          <v-btn color="primary" :loading="requesting" @click="onRequestLoan"
            >Invia richiesta</v-btn
          >
        </v-card-actions>
      </v-card>
    </v-dialog>

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
import { computed, reactive, ref, onMounted } from 'vue'
import { useApi } from '@/composables/useApi'
import { useLoanStatus } from '@/composables/useLoanStatus'
import { useAuthStore } from '@/stores/auth'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const api = useApi()
const { statusLabel, statusColor, statusIcon, formatDate } = useLoanStatus()
const auth = useAuthStore()
const book = ref<any>(null)
const library = ref<any>(null)
const loans = ref<any[]>([])
const loading = ref(true)

const libraryId = route.params.id as string
const bookId = route.params.bookId as string

const confirmDelete = ref(false)
const deleting = ref(false)
const snackbar = reactive({ show: false, text: '', color: 'success' })

const requestDialog = ref(false)
const requesting = ref(false)
const requestError = ref('')
const returnDate = ref('')
const updatingId = ref('')

const minReturnDate = computed(() => addDays(1))
const maxReturnDate = computed(() => addDays(90))

// Se non è il proprietario, il server restituisce solo le richieste dell'utente stesso.
const myLoan = computed(() =>
  loans.value.find((l) => l.status === 'Pending' || l.status === 'Approved'),
)

const pendingCount = computed(() => loans.value.filter((l) => l.status === 'Pending').length)
const activeLoansCount = computed(() => loans.value.filter((l) => l.status === 'Approved').length)

// Il server manda l'indirizzo solo a chi ne ha diritto: quando manca resta la città.
const pickupPosition = computed(() => {
  const l = library.value
  if (!l) return ''
  return l.address ?? [l.city, l.countryCode].filter(Boolean).join(', ')
})

/** Data a N giorni da oggi nel formato che si aspetta un input type="date". */
function addDays(days: number) {
  const date = new Date()
  date.setDate(date.getDate() + days)
  return date.toISOString().slice(0, 10)
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

  const libraryResponse = await api.apiFetch(`/library/${libraryId}`)
  if (libraryResponse.ok) {
    library.value = await libraryResponse.json()
  }

  await loadLoans()
})

// L'endpoint dei prestiti richiede una sessione: da anonimo non c'è niente da chiedere.
async function loadLoans() {
  if (!auth.isAuthenticated) return

  const res = await api.apiFetch(`/loan/book/${bookId}`)
  if (res.ok) {
    loans.value = await res.json()
  }
}

function openRequestDialog() {
  requestError.value = ''
  returnDate.value = addDays(30)
  requestDialog.value = true
}

async function onRequestLoan() {
  if (!returnDate.value) {
    requestError.value = 'Indica una data di rientro.'
    return
  }

  requesting.value = true
  requestError.value = ''
  try {
    const res = await api.apiFetch('/loan', {
      method: 'POST',
      body: JSON.stringify({ bookId, returnDate: returnDate.value }),
    })

    if (res.status === 409) {
      requestError.value = 'Hai già una richiesta in corso per questo libro.'
      return
    }
    if (!res.ok) throw new Error(`HTTP ${res.status}`)

    requestDialog.value = false
    notify('Richiesta inviata.')
    await loadLoans()

    // La libreria si ricarica perché con il prestito può cambiare l'indirizzo mostrato.
    const libraryResponse = await api.apiFetch(`/library/${libraryId}`)
    if (libraryResponse.ok) {
      library.value = await libraryResponse.json()
    }
  } catch {
    requestError.value = 'Richiesta non riuscita, riprova.'
  } finally {
    requesting.value = false
  }
}

async function updateStatus(loan: any, status: string) {
  updatingId.value = loan.id
  try {
    const res = await api.apiFetch(`/loan/${loan.id}`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    await loadLoans()
  } catch {
    notify('Aggiornamento non riuscito.', 'error')
  } finally {
    updatingId.value = ''
  }
}

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
