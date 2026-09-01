<template>
  <!--
    Riquadro statistiche visibile al solo proprietario del patrimonio.
    Il controllo vero è lato server (gli endpoint /stats rispondono 403 a chi non
    possiede la libreria).
  -->
  <v-card v-if="!forbidden" variant="flat" class="rounded-lg">
    <v-card-title class="d-flex flex-wrap align-center ga-2">
      <span>{{ title }}</span>
      <v-spacer />
      <v-select
        v-model="days"
        :items="PERIOD_OPTIONS"
        density="compact"
        variant="outlined"
        hide-details
        label="Periodo"
        style="max-width: 220px"
        @update:model-value="load"
      />
    </v-card-title>

    <v-card-text>
      <v-skeleton-loader v-if="loading" type="image" />

      <v-alert v-else-if="error" type="error" variant="tonal" density="compact">
        {{ error }}
      </v-alert>

      <template v-else>
        <div class="d-flex align-center ga-2 mb-3">
          <v-chip size="small" variant="tonal" prepend-icon="mdi-eye-outline">
            {{ totalViews }} visite negli {{ periodLabel }}
          </v-chip>
        </div>

        <views-chart
          :labels="labels"
          :datasets="[{ label: 'Visite', data: values }]"
          :period-label="periodLabel"
        />
      </template>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useApi } from '@/composables/useApi'
import { PERIOD_OPTIONS, useStatsPeriod, type DateStats } from '@/composables/useStatsPeriod'
import ViewsChart from '@/components/views-chart.vue'

const props = defineProps<{
  libraryId: string
  /** Se valorizzato mostra le statistiche del singolo libro invece che della libreria. */
  bookId?: string
}>()

const api = useApi()
const { days, query, periodLabel, toLabels, toValues, total } = useStatsPeriod(30)

const stats = ref<DateStats[]>([])
const loading = ref(true)
const error = ref('')
const forbidden = ref(false)

const title = computed(() =>
  props.bookId ? 'Visualizzazioni del libro' : 'Visualizzazioni della libreria',
)
const labels = computed(() => toLabels(stats.value))
const values = computed(() => toValues(stats.value))
const totalViews = computed(() => total(stats.value))

const endpoint = computed(() =>
  props.bookId
    ? `/library/${props.libraryId}/books/${props.bookId}/stats`
    : `/library/${props.libraryId}/stats`,
)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const response = await api.apiFetch(`${endpoint.value}?${query.value}`)

    if (response.status === 403 || response.status === 401) {
      forbidden.value = true
      return
    }

    if (!response.ok) {
      error.value = 'Statistiche non disponibili al momento.'
      return
    }

    stats.value = (await response.json()) as DateStats[]
  } catch (e) {
    console.error('Errore nel caricamento delle statistiche:', e)
    error.value = 'Statistiche non disponibili al momento.'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
