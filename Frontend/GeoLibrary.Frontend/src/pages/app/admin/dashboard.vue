<template>
  <!--
    Pannello di amministrazione: metriche sull'intera piattaforma, non sul
    patrimonio del singolo utente. La rotta è filtrata dal router (meta.requiresRole),
    ma l'autorizzazione effettiva è la policy "Admin" sul DashboardController.
  -->
  <v-container>
    <h1 class="text-h5 font-weight-bold mb-4">Amministrazione</h1>

    <v-alert v-if="forbidden" type="warning" variant="tonal" class="mb-4">
      Non hai i permessi necessari per consultare queste metriche.
    </v-alert>

    <template v-else>
      <!-- Contatori complessivi -->
      <v-row>
        <v-col v-for="counter in counterCards" :key="counter.label" cols="6" md="3">
          <v-card variant="tonal" class="rounded-lg h-100">
            <v-card-text>
              <div class="d-flex align-center ga-2 mb-1">
                <v-icon :icon="counter.icon" size="20" />
                <span class="text-body-2 text-medium-emphasis">{{ counter.label }}</span>
              </div>
              <div class="text-h4 font-weight-bold">
                <v-progress-circular v-if="loadingCounters" indeterminate size="24" />
                <template v-else>{{ counter.value }}</template>
              </div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <!-- Andamento delle visite -->
      <v-row>
        <v-col cols="12">
          <v-card variant="flat" class="rounded-lg">
            <v-card-title class="d-flex flex-wrap align-center ga-2">
              <span>Visite nel tempo</span>
              <v-spacer />
              <v-select
                v-model="days"
                :items="PERIOD_OPTIONS"
                density="compact"
                variant="outlined"
                hide-details
                label="Periodo"
                style="max-width: 220px"
                @update:model-value="loadPeriodData"
              />
            </v-card-title>
            <v-card-text>
              <v-skeleton-loader v-if="loadingViews" type="image" />
              <v-alert v-else-if="viewsError" type="error" variant="tonal" density="compact">
                {{ viewsError }}
              </v-alert>
              <views-chart
                v-else
                :labels="labels"
                :datasets="chartSeries"
                :period-label="periodLabel"
              />
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <!-- Libri più visualizzati -->
      <v-row>
        <v-col cols="12">
          <v-card variant="flat" class="rounded-lg">
            <v-card-title>Libri più visualizzati ({{ periodLabel }})</v-card-title>
            <v-card-text>
              <v-skeleton-loader v-if="loadingTopBooks" type="table" />
              <p v-else-if="topBooks.length === 0" class="text-medium-emphasis mb-0">
                Nessuna visualizzazione registrata nel periodo selezionato.
              </p>
              <v-table v-else density="comfortable">
                <caption class="text-caption text-medium-emphasis text-left pa-1">
                  {{
                    topBooksCaption
                  }}
                </caption>
                <thead>
                  <tr>
                    <th scope="col">Titolo</th>
                    <th scope="col">Autore</th>
                    <th scope="col">Libreria</th>
                    <th scope="col" class="text-end">Visite</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="book in topBooks" :key="book.id">
                    <th scope="row" class="font-weight-regular">
                      <router-link :to="`/libraries/${book.libraryId}/book/${book.id}`">
                        {{ book.title }}
                      </router-link>
                    </th>
                    <td>{{ book.author }}</td>
                    <td>{{ book.libraryName }}</td>
                    <td class="text-end">{{ book.viewsCount }}</td>
                  </tr>
                </tbody>
              </v-table>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>
    </template>
  </v-container>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useApi } from '@/composables/useApi'
import { PERIOD_OPTIONS, useStatsPeriod, type DateStats } from '@/composables/useStatsPeriod'
import ViewsChart from '@/components/views-chart.vue'

interface Counters {
  usersCount: number
  librariesCount: number
  booksCount: number
  loanRequestsCount: number
}

interface TopBook {
  id: string
  libraryId: string
  title: string
  author: string
  libraryName: string
  viewsCount: number
}

const api = useApi()
const { days, query, periodLabel, toLabels, toValues } = useStatsPeriod(30)

const counters = ref<Counters | null>(null)
const libraryViews = ref<DateStats[]>([])
const bookViews = ref<DateStats[]>([])
const topBooks = ref<TopBook[]>([])

const loadingCounters = ref(true)
const loadingViews = ref(true)
const loadingTopBooks = ref(true)
const viewsError = ref('')
const forbidden = ref(false)

const counterCards = computed(() => [
  { label: 'Utenti', icon: 'mdi-account-group', value: counters.value?.usersCount ?? 0 },
  { label: 'Librerie', icon: 'mdi-bookshelf', value: counters.value?.librariesCount ?? 0 },
  { label: 'Libri', icon: 'mdi-book-multiple', value: counters.value?.booksCount ?? 0 },
  { label: 'Prestiti', icon: 'mdi-swap-horizontal', value: counters.value?.loanRequestsCount ?? 0 },
])

// Le due serie condividono l'asse dei tempi: il backend riempie i giorni senza visite.
const labels = computed(() => toLabels(libraryViews.value))
const chartSeries = computed(() => [
  { label: 'Visite librerie', data: toValues(libraryViews.value) },
  { label: 'Visite libri', data: toValues(bookViews.value) },
])

/** Didascalia della tabella: descrive il contenuto anche a chi usa uno screen reader. */
const topBooksCaption = computed(
  () => `I ${topBooks.value.length} libri più consultati negli ${periodLabel.value}.`,
)

/** Un 403 qui vale per tutta la pagina: l'utente non ha il ruolo admin. */
function handleForbidden(response: Response) {
  if (response.status === 403 || response.status === 401) {
    forbidden.value = true
    return true
  }
  return false
}

async function loadCounters() {
  loadingCounters.value = true
  try {
    const response = await api.apiFetch('/dashboard/counters')
    if (handleForbidden(response) || !response.ok) return
    counters.value = (await response.json()) as Counters
  } catch (e) {
    console.error('Errore nel caricamento dei contatori:', e)
  } finally {
    loadingCounters.value = false
  }
}

async function loadViews() {
  loadingViews.value = true
  viewsError.value = ''
  try {
    const response = await api.apiFetch(`/dashboard/views?${query.value}`)
    if (handleForbidden(response)) return
    if (!response.ok) {
      viewsError.value = 'Statistiche non disponibili al momento.'
      return
    }
    const data = (await response.json()) as {
      libraryViews: DateStats[]
      bookViews: DateStats[]
    }
    libraryViews.value = data.libraryViews
    bookViews.value = data.bookViews
  } catch (e) {
    console.error('Errore nel caricamento delle visite:', e)
    viewsError.value = 'Statistiche non disponibili al momento.'
  } finally {
    loadingViews.value = false
  }
}

async function loadTopBooks() {
  loadingTopBooks.value = true
  try {
    const response = await api.apiFetch(`/dashboard/top-books?${query.value}&limit=10`)
    if (handleForbidden(response) || !response.ok) return
    topBooks.value = (await response.json()) as TopBook[]
  } catch (e) {
    console.error('Errore nel caricamento dei libri più visti:', e)
  } finally {
    loadingTopBooks.value = false
  }
}

/** Ricaricato a ogni cambio di periodo; i contatori non dipendono dall'intervallo. */
function loadPeriodData() {
  return Promise.all([loadViews(), loadTopBooks()])
}

onMounted(() => Promise.all([loadCounters(), loadPeriodData()]))
</script>
