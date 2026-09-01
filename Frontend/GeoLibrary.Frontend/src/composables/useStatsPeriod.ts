import { ref, computed } from 'vue'
import type { Ref } from 'vue'

/**
 * Periodo selezionabile nei riquadri statistiche (owner e amministrazione).
 * Isolato in un composable perché la stessa logica di date serve sia alla card
 * del proprietario sia al pannello di amministrazione.
 */

export interface DateStats {
  date: string
  viewsCount: number
}

export const PERIOD_OPTIONS = [
  { title: 'Ultimi 7 giorni', value: 7 },
  { title: 'Ultimi 30 giorni', value: 30 },
  { title: 'Ultimi 90 giorni', value: 90 },
]

/** Formatta una data in `YYYY-MM-DD`, il formato atteso dai parametri from/to. */
function toIsoDate(date: Date) {
  return date.toISOString().slice(0, 10)
}

export function useStatsPeriod(defaultDays = 30) {
  const days: Ref<number> = ref(defaultDays)

  /** Intervallo chiuso che termina oggi; `days` include il giorno corrente. */
  const range = computed(() => {
    const to = new Date()
    const from = new Date()
    from.setDate(to.getDate() - (days.value - 1))
    return { from: toIsoDate(from), to: toIsoDate(to) }
  })

  const query = computed(() => `from=${range.value.from}&to=${range.value.to}`)

  const periodLabel = computed(() => `ultimi ${days.value} giorni`)

  /** Etichette compatte per l'asse X: giorno e mese bastano su una finestra <= 1 anno. */
  function toLabels(stats: DateStats[]) {
    return stats.map((entry) => {
      const [, month, day] = entry.date.split('-')
      return `${day}/${month}`
    })
  }

  function toValues(stats: DateStats[]) {
    return stats.map((entry) => entry.viewsCount)
  }

  function total(stats: DateStats[]) {
    return stats.reduce((sum, entry) => sum + entry.viewsCount, 0)
  }

  return { days, range, query, periodLabel, toLabels, toValues, total }
}
