<template>
  <v-card variant="flat" class="rounded-lg">
    <v-card-title class="text-h5 px-0">Trova un libro vicino a te</v-card-title>

    <v-alert v-if="error" type="error" variant="tonal" class="mb-4" role="alert">
      {{ error }}
    </v-alert>

    <v-card variant="outlined" class="rounded-lg overflow-hidden">
      <!-- Barra dei controlli: testo cercato, modo di delimitare l'area, bottone di ricerca -->
      <div class="d-flex flex-wrap ga-3 pa-4 bg-surface-light">
        <v-text-field
          v-model="query"
          placeholder="Titolo, autore o ISBN — lascia vuoto per vedere tutto"
          prepend-inner-icon="mdi-magnify"
          variant="outlined"
          density="compact"
          hide-details
          clearable
          style="min-width: 260px"
          @keyup.enter="search"
        />

        <v-btn-toggle v-model="mode" mandatory density="compact" variant="outlined" divided>
          <v-btn value="radius" prepend-icon="mdi-radius-outline">Raggio</v-btn>
          <v-btn value="draw" prepend-icon="mdi-shape-polygon-plus">Area disegnata</v-btn>
        </v-btn-toggle>

        <v-btn color="primary" :loading="loading" @click="search">Cerca</v-btn>
      </div>

      <v-divider />

      <!-- Barra del modo attivo -->
      <div class="d-flex align-center flex-wrap ga-4 px-4 py-2" style="min-height: 56px">
        <template v-if="mode === 'radius'">
          <span class="text-body-2 text-medium-emphasis">Entro</span>
          <v-slider
            v-model="radiusKm"
            :min="0.5"
            :max="12"
            :step="0.5"
            hide-details
            density="compact"
            style="max-width: 240px"
            @end="search"
          />
          <b class="text-body-2">{{ formatKm(radiusKm) }}</b>
          <span class="text-caption text-medium-emphasis">
            trascina il segnaposto per spostare il centro della ricerca
          </span>
        </template>

        <template v-else>
          <v-btn
            variant="tonal"
            size="small"
            :color="drawing ? 'primary' : undefined"
            prepend-icon="mdi-draw"
            @click="startDrawing"
          >
            {{ drawing ? 'Clicca sulla mappa…' : 'Disegna area' }}
          </v-btn>
          <v-btn variant="text" size="small" @click="clearArea">Cancella</v-btn>
          <span class="text-caption text-medium-emphasis">
            clicca sulla mappa per mettere i vertici, doppio clic per chiudere l'area
          </span>
        </template>
      </div>

      <v-divider />

      <v-row no-gutters>
        <!-- Mappa -->
        <v-col cols="12" md="7">
          <div style="height: 400px">
            <l-map
              v-model:zoom="zoom"
              :center="center"
              :use-global-leaflet="false"
              :options="mapOptions"
              @click="onMapClick"
              @dblclick="closeArea"
            >
              <l-tile-layer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                layer-type="base"
                name="OpenStreetMap"
                attribution="&copy; OpenStreetMap contributors"
              />

              <!-- Centro della ricerca: si trascina per spostare il cerchio -->
              <l-marker :lat-lng="center" :draggable="true" @dragend="onCenterDragged" />

              <l-circle
                v-if="mode === 'radius'"
                :lat-lng="center"
                :radius="radiusKm * 1000"
                color="#1266c9"
                :weight="2"
                :fill-opacity="0.08"
              />

              <!-- Area disegnata: prima una spezzata, poi il poligono chiuso -->
              <l-polyline
                v-if="mode === 'draw' && !areaClosed && points.length > 1"
                :lat-lngs="points"
                color="#1266c9"
                :weight="2"
                :dash-array="'5 4'"
              />
              <l-polygon
                v-if="mode === 'draw' && areaClosed"
                :lat-lngs="points"
                color="#1266c9"
                :weight="2"
                :fill-opacity="0.08"
              />

              <!-- Una libreria per posizione, con i titoli che contiene -->
              <l-marker
                v-for="library in resultLibraries"
                :key="library.id"
                :lat-lng="[library.latitude, library.longitude]"
              >
                <l-tooltip>{{ library.name }}</l-tooltip>
              </l-marker>
            </l-map>
          </div>
        </v-col>

        <!-- Risultati -->
        <v-col cols="12" md="5" class="d-flex flex-column border-s" style="height: 400px">
          <div class="px-4 py-3 border-b text-body-2 text-medium-emphasis">
            <b class="text-h6 text-high-emphasis">{{ noArea ? '—' : results.length }}</b>
            libri in quest'area
          </div>

          <v-skeleton-loader v-if="loading" type="list-item-avatar-two-line@4" />

          <div v-else-if="!results.length" class="pa-6 text-body-2 text-medium-emphasis">
            {{ emptyMessage }}
          </div>

          <v-list v-else class="overflow-auto py-0" lines="two">
            <v-list-item
              v-for="book in results"
              :key="book.id"
              @click="goToBook(book)"
              class="border-b"
            >
              <template #prepend>
                <v-avatar rounded="0" size="38" color="surface-light">
                  <v-img v-if="book.coverThumbnailUrl" :src="book.coverThumbnailUrl" cover />
                  <span v-else class="text-caption">{{ initials(book.title) }}</span>
                </v-avatar>
              </template>

              <v-list-item-title>{{ book.title }}</v-list-item-title>
              <v-list-item-subtitle>{{ book.author }}</v-list-item-subtitle>

              <template #append>
                <div class="d-flex flex-column align-end ga-1">
                  <span class="text-body-2 text-primary">{{ formatDistance(book.distance) }}</span>
                  <v-chip size="x-small" variant="tonal">{{ book.libraryName }}</v-chip>
                </div>
              </template>
            </v-list-item>
          </v-list>
        </v-col>
      </v-row>
    </v-card>
  </v-card>
</template>

<script setup lang="ts">
import 'leaflet/dist/leaflet.css'
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import {
  LMap,
  LTileLayer,
  LMarker,
  LCircle,
  LPolyline,
  LPolygon,
  LTooltip,
} from '@vue-leaflet/vue-leaflet'
import { Icon, latLng } from 'leaflet'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'
import { useGeolocation } from '@vueuse/core'
import { useApi } from '@/composables/useApi'

// Fix noto Leaflet + Vite: senza questo le icone del marker non si caricano
delete (Icon.Default.prototype as any)._getIconUrl
Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
})

// Le librerie dell'utente servono solo come ripiego per il centro della mappa,
// quando il browser non dà la posizione.
const props = defineProps<{
  libraries?: Array<any>
}>()

const router = useRouter()
const { apiFetch } = useApi()

const mode = ref<'radius' | 'draw'>('radius')
const query = ref('')
const radiusKm = ref(3)

const zoom = ref(5)
const center = ref<[number, number]>([42.5, 12.5]) // centro Italia di default

// Il doppio clic chiude l'area disegnata, quindi non deve anche zoomare.
const mapOptions = { doubleClickZoom: false }

const points = ref<[number, number][]>([])
const drawing = ref(false)
const areaClosed = ref(false)

const results = ref<any[]>([])
const loading = ref(false)
const error = ref('')

/* ---------- Posizione di partenza ---------- */

const { coords, isSupported, error: geoError } = useGeolocation()

/* ---------- Ricerca ---------- */

const noArea = computed(() => mode.value === 'draw' && !areaClosed.value)

const emptyMessage = computed(() => {
  if (noArea.value) return "Disegna un'area sulla mappa per vedere i libri che contiene."
  return "Nessun libro in quest'area: prova ad allargare il raggio o a spostare il centro."
})

// Un segnaposto per libreria: più libri della stessa libreria condividono la posizione.
const resultLibraries = computed(() => {
  const map = new Map<string, any>()
  for (const book of results.value) {
    if (!map.has(book.libraryId)) {
      map.set(book.libraryId, {
        id: book.libraryId,
        name: book.libraryName,
        latitude: book.latitude,
        longitude: book.longitude,
      })
    }
  }
  return [...map.values()]
})

async function search() {
  if (noArea.value) {
    results.value = []
    return
  }

  loading.value = true
  error.value = ''

  try {
    const response =
      mode.value === 'radius'
        ? await apiFetch(
            `/book/search/radius?latitude=${center.value[0]}&longitude=${center.value[1]}` +
              `&radiusKilometers=${radiusKm.value}&search=${encodeURIComponent(query.value ?? '')}`,
          )
        : await apiFetch('/book/search/polygon', {
            method: 'POST',
            body: JSON.stringify({
              coordinates: points.value.map(([latitude, longitude]) => ({ latitude, longitude })),
              search: query.value ?? '',
            }),
          })

    if (!response.ok) {
      error.value = 'Ricerca non riuscita, riprova.'
      results.value = []
      return
    }

    // La distanza si calcola qui: il centro della ricerca ce l'ha già il client.
    const origin = latLng(center.value)
    const books = await response.json()
    results.value = books
      .map((b: any) => ({ ...b, distance: origin.distanceTo([b.latitude, b.longitude]) }))
      .sort((a: any, b: any) => a.distance - b.distance)
  } catch {
    error.value = 'Ricerca non riuscita, riprova.'
    results.value = []
  } finally {
    loading.value = false
  }
}

watch(mode, () => {
  clearArea()
  search()
})

/* ---------- Posizione di partenza ---------- */

// Le librerie arrivano dalla Dashboard con una chiamata, quindi all'avvio possono non esserci
// ancora: si cerca subito col centro di default e si ricentra appena la risposta arriva.
function fallbackCenter() {
  search()

  // Dichiarato prima del watch: il primo giro è immediato e potrebbe già doverlo fermare.
  let stopLibrariesWatch: (() => void) | undefined
  stopLibrariesWatch = watch(
    () => props.libraries,
    (libraries) => {
      const library = libraries?.find((l) => l.latitude != null)
      if (!library) return

      center.value = [library.latitude, library.longitude]
      zoom.value = 13
      stopLibrariesWatch?.()
      search()
    },
    { immediate: true },
  )
}

// Si aspetta la prima posizione valida, poi il watch si ferma: da lì in poi
// il centro lo decide l'utente trascinando il segnaposto.
let positionFound = false
let stopGeoWatch: (() => void) | undefined

stopGeoWatch = watch(
  [coords, geoError],
  ([c, err]) => {
    if (positionFound) return

    if (!isSupported.value || err) {
      positionFound = true
      stopGeoWatch?.()
      fallbackCenter()
      return
    }

    if (Number.isFinite(c.latitude) && (c.latitude !== 0 || c.longitude !== 0)) {
      positionFound = true
      center.value = [c.latitude, c.longitude]
      zoom.value = 13
      stopGeoWatch?.()
      search()
    }
  },
  { immediate: true },
)

function onCenterDragged(event: any) {
  const position = event.target.getLatLng()
  center.value = [position.lat, position.lng]
  if (mode.value === 'radius') search()
}

/* ---------- Disegno dell'area ---------- */

function startDrawing() {
  drawing.value = true
  points.value = []
  areaClosed.value = false
  results.value = []
}

function clearArea() {
  drawing.value = false
  points.value = []
  areaClosed.value = false
}

function onMapClick(event: any) {
  if (mode.value !== 'draw' || !drawing.value) return
  points.value = [...points.value, [event.latlng.lat, event.latlng.lng]]
}

function closeArea() {
  if (mode.value !== 'draw' || !drawing.value || points.value.length < 3) return
  drawing.value = false
  areaClosed.value = true
  search()
}

/* ---------- Formattazione ---------- */

function formatKm(km: number) {
  return `${km.toFixed(1).replace('.', ',')} km`
}

function formatDistance(meters: number) {
  return meters < 1000 ? `${Math.round(meters)} m` : formatKm(meters / 1000)
}

function initials(title: string) {
  return title
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((w) => w[0]?.toUpperCase())
    .join('')
}

function goToBook(book: any) {
  router.push(`/app/libraries/${book.libraryId}/book/${book.id}`)
}
</script>
