<template>
  <v-form ref="formRef" @submit.prevent="onSubmit" validate-on="submit">
    <v-alert v-if="errorSummary" type="error" variant="tonal" class="mb-6" role="alert">
      {{ errorSummary }}
    </v-alert>

    <v-card variant="flat">
      <v-card-title>Informazioni principali</v-card-title>

      <v-card-text>
        <v-text-field
          v-model="form.name"
          label="Nome della libreria"
          :rules="[rules.required, rules.maxLen(120)]"
          counter="120"
          maxlength="120"
          variant="outlined"
          autocomplete="off"
          required
          aria-required="true"
          autofocus
        />

        <v-textarea
          v-model="form.description"
          label="Descrizione"
          hint="Una breve descrizione visibile nella scheda della libreria"
          persistent-hint
          :rules="[rules.maxLen(500)]"
          counter="500"
          maxlength="500"
          variant="outlined"
          rows="3"
          auto-grow
        />

        <v-checkbox
          v-model="form.isHidden"
          label="Nascondi la libreria agli altri utenti"
          hint="Non comparirà nelle ricerche sulla mappa e resterà visibile solo a te"
          persistent-hint
        />
      </v-card-text>
    </v-card>

    <v-card variant="flat">
      <v-card-title>Posizione</v-card-title>
      <v-card-text>
        <v-autocomplete
          v-model="selectedPlace"
          v-model:search="addressSearch"
          :items="suggestions"
          :loading="geoLoading"
          item-title="label"
          return-object
          label="Cerca indirizzo"
          placeholder="Inizia a digitare (min. 3 caratteri)…"
          variant="outlined"
          prepend-inner-icon="mdi-map-marker"
          no-filter
          :rules="[() => form.lat != null || 'Seleziona un indirizzo dall’elenco']"
          auto-select-first
          autocomplete="off"
          required
          aria-required="true"
          @update:model-value="onPlaceSelected"
        >
          <template #no-data>
            <v-list-item>
              <v-list-item-title class="text-medium-emphasis">
                {{ addressSearch.length < 3 ? 'Digita almeno 3 caratteri' : 'Nessun risultato' }}
              </v-list-item-title>
            </v-list-item>
          </template>
        </v-autocomplete>

        <v-row>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="form.city"
              label="Città"
              variant="outlined"
              autocomplete="address-level2"
            />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="form.postalCode"
              label="CAP"
              variant="outlined"
              inputmode="numeric"
              autocomplete="postal-code"
            />
          </v-col>
          <v-col cols="12">
            <!-- Nazione: ricavata dal geocoding, richiesta dal backend. Sola lettura. -->
            <v-text-field
              v-model="form.country"
              label="Nazione"
              variant="outlined"
              readonly
              :rules="[rules.required]"
              autocomplete="country-name"
            />
          </v-col>
        </v-row>
        <div
          class="mt-4 rounded overflow-hidden"
          style="height: 320px"
          role="img"
          :aria-label="
            form.lat != null
              ? `Posizione selezionata: ${form.address}`
              : 'Nessuna posizione selezionata'
          "
        >
          <l-map
            ref="mapRef"
            v-model:zoom="zoom"
            :center="center"
            :use-global-leaflet="false"
            :options="mapOptions"
          >
            <l-tile-layer
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
              layer-type="base"
              name="OpenStreetMap"
              attribution="&copy; OpenStreetMap contributors"
            />
            <l-marker v-if="form.lat != null && form.lon != null" :lat-lng="[form.lat, form.lon]" />
          </l-map>
        </div>
        <p class="text-caption text-medium-emphasis mt-1">
          La mappa mostra la posizione dell'indirizzo selezionato.
        </p>
      </v-card-text>
    </v-card>

    <v-card variant="flat">
      <v-card-title>Immagine di copertina</v-card-title>

      <v-card-text>
        <!-- In modifica si mostra l'immagine già salvata, finché non se ne sceglie una nuova. -->
        <v-img
          v-if="library?.imageUrl && !form.imageFile"
          :src="library.imageUrl"
          :alt="`Immagine attuale di ${form.name}`"
          max-height="200"
          class="mb-4 rounded"
          cover
        />

        <!--
          v-file-upload: dropzone nativa di Vuetify. Gestisce drag&drop, click,
          anteprima e rimozione del file, evitando markup e CSS custom.
        -->
        <v-file-upload
          v-model="form.imageFile"
          accept="image/jpeg,image/png"
          icon="mdi-image-plus"
          title="Trascina un'immagine o premi per selezionarla"
          subtitle="JPG o PNG, max 5 MB"
          density="comfortable"
          clearable
        />

        <p v-if="imageError" class="text-error text-caption mt-2" role="alert">
          {{ imageError }}
        </p>
      </v-card-text>
    </v-card>

    <!-- Azioni: bottone primario chiaro + annulla. Stato di loading bloccante. -->
    <div class="d-flex justify-end ga-3">
      <v-btn variant="text" :disabled="saving" @click="emit('cancel')"> Annulla </v-btn>
      <v-btn type="submit" color="primary" :loading="saving" prepend-icon="mdi-content-save">
        {{ submitLabel }}
      </v-btn>
    </div>
  </v-form>
</template>

<script setup lang="ts">
import 'leaflet/dist/leaflet.css'
import { computed, reactive, ref, watch, useTemplateRef } from 'vue'
import { LMap, LTileLayer, LMarker } from '@vue-leaflet/vue-leaflet'
import { Icon } from 'leaflet'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'
import { useDebounceFn } from '@vueuse/core'
import { useGeocoding, type GeoResult } from '@/composables/useGeocoding'

// Fix noto Leaflet + Vite: senza questo le icone del marker non si caricano
delete (Icon.Default.prototype as any)._getIconUrl
Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
})

// Il componente non conosce l'API: raccoglie i dati e li passa alla pagina che lo usa.
const props = defineProps<{
  library?: any
  saving?: boolean
  submitLabel?: string
  error?: string
}>()

const emit = defineEmits<{
  submit: [payload: any, image?: File]
  cancel: []
}>()

const { search } = useGeocoding()

const formRef = useTemplateRef<any>('formRef')

const MAX_IMAGE_BYTES = 5 * 1024 * 1024

const form = reactive({
  name: '',
  description: '',
  address: '',
  city: '',
  postalCode: '',
  country: '',
  countryCode: '',
  lat: null as number | null,
  lon: null as number | null,
  isHidden: false,
  imageFile: undefined as File | undefined,
})

const imageError = ref('')
const localError = ref('')
const errorSummary = computed(() => localError.value || props.error || '')

const rules = {
  required: (v: unknown) => !!v || 'Campo obbligatorio',
  maxLen: (n: number) => (v: string) => !v || v.length <= n || `Massimo ${n} caratteri`,
}

/* ---------- Geocoding (Nominatim) ---------- */

const addressSearch = ref('')
const suggestions = ref<GeoResult[]>([])
const selectedPlace = ref<GeoResult | null>(null)
const geoLoading = ref(false)

const zoom = ref(5)
const center = ref<[number, number]>([42.5, 12.5]) // centro Italia di default

// Mappa puramente indicativa: ogni interazione è disattivata
const mapOptions = {
  dragging: false,
  scrollWheelZoom: false,
  doubleClickZoom: false,
  boxZoom: false,
  keyboard: false,
  touchZoom: false,
  zoomControl: false,
  attributionControl: true,
}

let geoController: AbortController | null = null

// Debounce ~500ms per rispettare il rate limit di Nominatim (1 req/s)
const runSearch = useDebounceFn(async (q: string) => {
  geoController?.abort()
  geoController = new AbortController()
  geoLoading.value = true
  try {
    suggestions.value = await search(q, geoController.signal)
  } catch (e) {
    if ((e as Error).name !== 'AbortError') suggestions.value = []
  } finally {
    geoLoading.value = false
  }
}, 500)

watch(addressSearch, (query) => {
  if (query && query !== selectedPlace.value?.label) runSearch(query)
})

function applyPlace(p: GeoResult) {
  form.address = p.label
  form.lat = p.lat
  form.lon = p.lon
  form.city = p.city ?? ''
  form.postalCode = p.postalCode ?? ''
  form.country = p.country ?? ''
  form.countryCode = p.countryCode ?? ''
  center.value = [p.lat, p.lon]
  zoom.value = 16
}

function onPlaceSelected(place: GeoResult | null) {
  if (place) {
    applyPlace(place)
  }
}

/* ---------- Immagine ---------- */
watch(
  () => form.imageFile,
  (file) => {
    imageError.value = ''
    if (!file) {
      return
    }
    if (!['image/jpeg', 'image/png'].includes(file.type)) {
      imageError.value = 'Formato non supportato: usa JPG o PNG.'
      form.imageFile = undefined
      return
    }
    if (file.size > MAX_IMAGE_BYTES) {
      imageError.value = 'Immagine troppo grande: massimo 5 MB.'
      form.imageFile = undefined
    }
  },
)

/* ---------- Precompilazione in modifica ---------- */
if (props.library) {
  Object.assign(form, {
    name: props.library.name,
    description: props.library.description ?? '',
    address: props.library.address,
    city: props.library.city,
    postalCode: props.library.postalCode,
    country: props.library.country,
    countryCode: props.library.countryCode,
    lat: props.library.latitude,
    lon: props.library.longitude,
    isHidden: props.library.isHidden ?? false,
  })

  // selectedPlace va impostato prima di addressSearch: il watch confronta i due valori
  // e in questo modo non parte una ricerca inutile al primo render.
  selectedPlace.value = {
    label: props.library.address,
    lat: props.library.latitude,
    lon: props.library.longitude,
  }
  // v-autocomplete mostra solo le voci presenti in :items, quindi ci si mette il valore corrente
  suggestions.value = [selectedPlace.value]
  addressSearch.value = props.library.address

  center.value = [props.library.latitude, props.library.longitude]
  zoom.value = 16
}

async function onSubmit() {
  localError.value = ''
  const { valid } = await formRef.value!.validate()

  if (!valid) {
    localError.value = 'Controlla i campi evidenziati e riprova.'
    return
  }

  emit(
    'submit',
    {
      name: form.name,
      description: form.description,
      address: form.address,
      city: form.city,
      country: form.country,
      countryCode: form.countryCode,
      postalCode: form.postalCode,
      latitude: form.lat,
      longitude: form.lon,
      isHidden: form.isHidden,
    },
    form.imageFile,
  )
}
</script>
