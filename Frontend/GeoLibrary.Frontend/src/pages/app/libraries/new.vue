<template>
  <v-container class="py-8" style="max-width: 760px">
    <!-- Titolo di pagina: un solo h1 per pagina, utile a screen reader e SEO -->
    <h1 class="text-h4 mb-6">Crea una nuova libreria</h1>

    <!-- 
      Riepilogo errori in cima: quando la validazione fallisce, sposto il focus qui.
      role="alert" + aria-live fanno annunciare il messaggio dagli screen reader.
    -->
    <v-alert
      v-if="errorSummary"
      ref="errorAlert"
      type="error"
      variant="tonal"
      class="mb-6"
      role="alert"
      tabindex="-1"
    >
      {{ errorSummary }}
    </v-alert>

    <v-form ref="formRef" @submit.prevent="onSubmit" validate-on="submit">
      <!-- 
        Ogni gruppo logico è un <fieldset>/<legend> nativo: dà struttura semantica.
        Vuetify non lo fa da solo, quindi lo aggiungo a mano.
      -->
      <v-card variant="flat">
        <v-card-title>Informazioni principali</v-card-title>

        <v-card-text>
          <v-text-field
            v-model="form.title"
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
              <l-marker
                v-if="form.lat != null && form.lon != null"
                :lat-lng="[form.lat, form.lon]"
              />
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

          <!--
            Campo testo alternativo: ESSENZIALE per l'accessibilità delle immagini.
            Non lasciarlo opzionale "silenzioso": guida l'utente a descriverla.
          -->
          <v-text-field
            v-if="form.imageFile"
            v-model="form.imageAlt"
            label="Testo alternativo dell'immagine"
            hint="Descrivi l'immagine per chi usa screen reader (es. «Facciata della libreria con insegna verde»)"
            persistent-hint
            :rules="[rules.required]"
            variant="outlined"
            class="mt-4"
            required
            aria-required="true"
          />
        </v-card-text>
      </v-card>

      <!-- Azioni: bottone primario chiaro + annulla. Stato di loading bloccante. -->
      <div class="d-flex justify-end ga-3">
        <v-btn variant="text" :disabled="saving" @click="onCancel"> Annulla </v-btn>
        <v-btn type="submit" color="primary" :loading="saving" prepend-icon="mdi-content-save">
          Salva libreria
        </v-btn>
      </div>
    </v-form>

    <!-- feedback non bloccante post-salvataggio -->
    <v-snackbar v-model="snackbar.show" :color="snackbar.color" role="status">
      {{ snackbar.text }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import 'leaflet/dist/leaflet.css'
import { reactive, ref, watch, useTemplateRef } from 'vue'
import { useRouter } from 'vue-router'
import { LMap, LTileLayer, LMarker } from '@vue-leaflet/vue-leaflet'
import { Icon } from 'leaflet'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'
import { useDebounceFn } from '@vueuse/core'
import { useApi } from '@/composables/useApi'
import { useGeocoding, type GeoResult } from '@/composables/useGeocoding'

// Fix noto Leaflet + Vite: senza questo le icone del marker non si caricano
delete (Icon.Default.prototype as any)._getIconUrl
Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
})

const router = useRouter()
const { apiFetch } = useApi()
const { search } = useGeocoding()

const formRef = useTemplateRef<any>('formRef')
const errorAlert = useTemplateRef<any>('errorAlert')

const MAX_IMAGE_BYTES = 5 * 1024 * 1024

const form = reactive({
  title: '',
  description: '',
  address: '',
  city: '',
  postalCode: '',
  country: '',
  countryCode: '',
  lat: null as number | null,
  lon: null as number | null,
  imageFile: undefined as File | undefined,
  imageAlt: '',
})

const saving = ref(false)
const imageError = ref('')
const errorSummary = ref('')
const snackbar = reactive({ show: false, text: '', color: 'success' })

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
      form.imageAlt = ''
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

async function onSubmit() {
  errorSummary.value = ''
  const { valid } = await formRef.value!.validate()

  if (!valid) {
    errorSummary.value = 'Controlla i campi evidenziati e riprova.'
    return
  }

  saving.value = true
  try {
    const payload = {
      name: form.title,
      address: form.address,
      city: form.city,
      country: form.country,
      countryCode: form.countryCode,
      postalCode: form.postalCode,
      latitude: form.lat,
      longitude: form.lon,
    }

    const res = await apiFetch('/library', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)

    snackbar.text = 'Libreria creata con successo.'
    snackbar.color = 'success'
    snackbar.show = true
    router.push('/app/libraries')
  } catch {
    errorSummary.value = 'Si è verificato un errore durante il salvataggio. Riprova.'
    snackbar.text = 'Salvataggio non riuscito.'
    snackbar.color = 'error'
    snackbar.show = true
  } finally {
    saving.value = false
  }
}

function onCancel() {
  router.back()
}
</script>
