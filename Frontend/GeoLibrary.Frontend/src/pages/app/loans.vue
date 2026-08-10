<template>
  <v-container class="py-8">
    <h1 class="text-h4 font-weight-bold mb-6">Prestiti</h1>

    <v-card variant="flat" class="rounded-lg">
      <v-tabs v-model="tab" color="primary">
        <v-tab value="mine">
          Le mie richieste
          <v-chip v-if="myActiveCount" size="x-small" class="ms-2">{{ myActiveCount }}</v-chip>
        </v-tab>
        <v-tab value="received">
          Richieste ricevute
          <v-chip v-if="pendingCount" size="x-small" color="warning" class="ms-2">
            {{ pendingCount }}
          </v-chip>
        </v-tab>
      </v-tabs>

      <v-divider />

      <v-tabs-window v-model="tab">
        <v-tabs-window-item value="mine">
          <v-data-table
            :headers="myHeaders"
            :items="myLoans"
            :loading="loading"
            item-value="id"
            density="comfortable"
          >
            <template #item.bookTitle="{ item }">
              <router-link :to="bookPath(item.libraryId, item.bookId)" class="text-primary">
                {{ item.bookTitle }}
              </router-link>
            </template>
            <template #item.libraryName="{ item }">
              <router-link :to="libraryPath(item.libraryId)" class="text-primary">
                {{ item.libraryName }}
              </router-link>
            </template>
            <template #item.bookingDate="{ item }">{{ formatDate(item.bookingDate) }}</template>
            <template #item.returnDate="{ item }">{{ formatDate(item.returnDate) }}</template>
            <template #item.status="{ item }">
              <v-chip
                size="small"
                variant="tonal"
                :color="isOverdue(item) ? 'error' : statusColor(item.status)"
                :prepend-icon="isOverdue(item) ? 'mdi-alert-outline' : statusIcon(item.status)"
              >
                {{ isOverdue(item) ? 'In ritardo' : statusLabel(item.status) }}
              </v-chip>
            </template>
            <template #no-data>
              <span class="text-medium-emphasis">
                Non hai ancora richiesto nessun libro in prestito.
              </span>
            </template>
          </v-data-table>
        </v-tabs-window-item>

        <v-tabs-window-item value="received">
          <v-data-table
            :headers="receivedHeaders"
            :items="receivedLoans"
            :loading="loading"
            item-value="id"
            density="comfortable"
          >
            <template #item.bookTitle="{ item }">
              <router-link :to="bookPath(item.libraryId, item.bookId)" class="text-primary">
                {{ item.bookTitle }}
              </router-link>
            </template>
            <template #item.bookingDate="{ item }">{{ formatDate(item.bookingDate) }}</template>
            <template #item.returnDate="{ item }">{{ formatDate(item.returnDate) }}</template>
            <template #item.status="{ item }">
              <v-chip
                size="small"
                variant="tonal"
                :color="isOverdue(item) ? 'error' : statusColor(item.status)"
                :prepend-icon="isOverdue(item) ? 'mdi-alert-outline' : statusIcon(item.status)"
              >
                {{ isOverdue(item) ? 'In ritardo' : statusLabel(item.status) }}
              </v-chip>
            </template>
            <template #item.actions="{ item }">
              <div class="d-flex justify-end ga-2">
                <template v-if="item.status === 'Pending'">
                  <v-btn
                    size="small"
                    variant="tonal"
                    color="success"
                    :loading="updatingId === item.id"
                    @click="updateStatus(item, 'Approved')"
                  >
                    Approva
                  </v-btn>
                  <v-btn
                    size="small"
                    variant="text"
                    :loading="updatingId === item.id"
                    @click="updateStatus(item, 'Rejected')"
                  >
                    Rifiuta
                  </v-btn>
                </template>

                <v-btn
                  v-else-if="item.status === 'Approved'"
                  size="small"
                  variant="tonal"
                  :loading="updatingId === item.id"
                  @click="updateStatus(item, 'Returned')"
                >
                  Segna restituito
                </v-btn>
              </div>
            </template>
            <template #no-data>
              <span class="text-medium-emphasis">
                Nessuno ha ancora chiesto in prestito i tuoi libri.
              </span>
            </template>
          </v-data-table>
        </v-tabs-window-item>
      </v-tabs-window>
    </v-card>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" role="status">
      {{ snackbar.text }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { computed, reactive, ref, onMounted } from 'vue'
import { useApi } from '@/composables/useApi'
import { useAppLink } from '@/composables/useAppLink'
import { useLoanStatus } from '@/composables/useLoanStatus'

const api = useApi()
const { libraryPath, bookPath } = useAppLink()
const { statusLabel, statusColor, statusIcon, isOverdue, formatDate } = useLoanStatus()

const tab = ref('mine')
const loading = ref(true)
const myLoans = ref<any[]>([])
const receivedLoans = ref<any[]>([])
const updatingId = ref('')
const snackbar = reactive({ show: false, text: '', color: 'success' })

// Le due tabelle hanno le stesse colonne, tranne il richiedente e le azioni che
// hanno senso solo per chi presta i libri.
const myHeaders = [
  { title: 'Libro', key: 'bookTitle' },
  { title: 'Biblioteca', key: 'libraryName' },
  { title: 'Richiesto il', key: 'bookingDate' },
  { title: 'Rientro entro', key: 'returnDate' },
  { title: 'Stato', key: 'status' },
]

const receivedHeaders = [
  { title: 'Libro', key: 'bookTitle' },
  { title: 'Richiedente', key: 'userDisplayName' },
  { title: 'Richiesto il', key: 'bookingDate' },
  { title: 'Rientro entro', key: 'returnDate' },
  { title: 'Stato', key: 'status' },
  { title: '', key: 'actions', sortable: false, align: 'end' as const },
]

const myActiveCount = computed(
  () => myLoans.value.filter((l) => l.status === 'Pending' || l.status === 'Approved').length,
)
const pendingCount = computed(
  () => receivedLoans.value.filter((l) => l.status === 'Pending').length,
)

function notify(text: string, color: 'success' | 'error' = 'success') {
  snackbar.text = text
  snackbar.color = color
  snackbar.show = true
}

onMounted(async () => {
  await Promise.all([loadMine(), loadReceived()])
  loading.value = false
})

async function loadMine() {
  const res = await api.apiFetch('/loan/mine')
  if (res.ok) {
    myLoans.value = await res.json()
  }
}

async function loadReceived() {
  const res = await api.apiFetch('/loan/received')
  if (res.ok) {
    receivedLoans.value = await res.json()
  }
}

async function updateStatus(loan: any, status: string) {
  updatingId.value = loan.id
  try {
    const res = await api.apiFetch(`/loan/${loan.id}`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    })
    // Il server rifiuta con 400 quando le copie sono già tutte in prestito:
    // il messaggio arriva come testo, quindi si mostra così com'è.
    if (!res.ok) throw new Error(await res.text())

    await loadReceived()
    notify('Richiesta aggiornata.')
  } catch (error) {
    notify((error as Error).message || 'Aggiornamento non riuscito.', 'error')
  } finally {
    updatingId.value = ''
  }
}
</script>
