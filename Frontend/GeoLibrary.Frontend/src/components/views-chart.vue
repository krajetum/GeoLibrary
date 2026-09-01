<template>
  <div>
    <!--
      Il <canvas> di Chart.js non ha un buon supporto all'accesbilità: il grafico
      espone quindi un riassunto testuale via aria-label e, subito sotto, gli
      stessi dati in una tabella apribile da tastiera.
    -->
    <div role="img" :aria-label="summary" class="chart-wrapper">
      <Line :data="chartData" :options="chartOptions" />
    </div>

    <details class="mt-2">
      <summary class="text-body-2 text-medium-emphasis cursor-pointer">
        Mostra i dati in tabella
      </summary>
      <v-table density="compact" class="mt-2">
        <caption class="text-caption text-medium-emphasis text-left pa-1">
          {{
            summary
          }}
        </caption>
        <thead>
          <tr>
            <th scope="col">Data</th>
            <th v-for="dataset in datasets" :key="dataset.label" scope="col" class="text-end">
              {{ dataset.label }}
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(label, index) in labels" :key="label">
            <th scope="row" class="font-weight-regular">{{ label }}</th>
            <td v-for="dataset in datasets" :key="dataset.label" class="text-end">
              {{ dataset.data[index] ?? 0 }}
            </td>
          </tr>
        </tbody>
      </v-table>
    </details>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import {
  CategoryScale,
  Chart as ChartJS,
  Filler,
  Legend,
  LineController,
  LineElement,
  LinearScale,
  PointElement,
  Tooltip,
} from 'chart.js'
import { Line } from 'vue-chartjs'
import { useTheme } from 'vuetify'

/**
 * Grafico a linee riutilizzabile per le serie storiche di visite.
 * Registra soltanto i moduli di Chart.js effettivamente usati, così il bundle
 * non si porta dietro tipi di grafico mai renderizzati (tree-shaking).
 */
ChartJS.register(
  LineController,
  LineElement,
  PointElement,
  LinearScale,
  CategoryScale,
  Tooltip,
  Legend,
  Filler,
)

export interface ChartSeries {
  label: string
  data: number[]
}

const props = defineProps<{
  /** Etichette dell'asse X, già formattate per la lettura (es. "01/09"). */
  labels: string[]
  /** Una o più serie da sovrapporre sullo stesso asse. */
  datasets: ChartSeries[]
  /** Descrizione del periodo, usata nel testo alternativo. */
  periodLabel?: string
}>()

const theme = useTheme()

/**
 * I colori arrivano dal tema Vuetify e non sono scritti a mano: se il tema
 * cambia (o si aggiunge quello scuro) il grafico resta coerente e leggibile.
 */
const FALLBACK_PALETTE = ['#1867C0', '#48A9A6', '#4CAF50', '#FB8C00']

const palette = computed<string[]>(() => {
  const colors = theme.current.value.colors
  // I token del tema possono essere descritti anche in forma non testuale:
  // teniamo solo i valori CSS validi e completiamo con la palette di riserva.
  const themeColors = [colors.primary, colors.info, colors.success, colors.warning]
  return themeColors.map((color, index) =>
    typeof color === 'string' ? color : (FALLBACK_PALETTE[index] ?? FALLBACK_PALETTE[0]!),
  )
})

const chartData = computed(() => ({
  labels: props.labels,
  datasets: props.datasets.map((series, index) => {
    const color = palette.value[index % palette.value.length]
    return {
      label: series.label,
      data: series.data,
      borderColor: color,
      backgroundColor: color,
      fill: false,
      tension: 0.3,
      pointRadius: props.labels.length > 60 ? 0 : 2,
    }
  }),
}))

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: { mode: 'index' as const, intersect: false },
  plugins: {
    // Con una sola serie la legenda non aggiunge informazione.
    legend: { display: props.datasets.length > 1 },
  },
  scales: {
    y: {
      beginAtZero: true,
      // Le visite sono numeri interi: niente tacche decimali.
      ticks: { precision: 0 },
    },
  },
}))

/** Riassunto leggibile da screen reader e riusato come didascalia della tabella. */
const summary = computed(() => {
  const totals = props.datasets
    .map((series) => `${series.label}: ${series.data.reduce((sum, value) => sum + value, 0)}`)
    .join(', ')
  const period = props.periodLabel ? ` (${props.periodLabel})` : ''
  return `Visite giornaliere${period}. Totali - ${totals}.`
})
</script>

<style scoped>
.chart-wrapper {
  position: relative;
  height: 260px;
}

.cursor-pointer {
  cursor: pointer;
}
</style>
