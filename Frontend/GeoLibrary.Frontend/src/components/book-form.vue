<template>
  <v-form ref="formRef" @submit.prevent="onSubmit" validate-on="submit">
    <v-alert v-if="errorSummary" type="error" variant="tonal" class="mb-6" role="alert">
      {{ errorSummary }}
    </v-alert>

    <v-card variant="flat">
      <v-card-title>Informazioni principali</v-card-title>

      <v-card-text>
        <v-text-field
          v-model="form.title"
          label="Nome"
          :rules="[rules.required, rules.maxLen(250)]"
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
          hint="Una breve descrizione visibile nella scheda del libro"
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
      <v-card-title>Informazioni aggiuntive</v-card-title>
      <v-card-text>
        <v-text-field
          v-model="form.isbn"
          label="ISBN"
          :rules="[rules.maxLen(20)]"
          counter="20"
          maxlength="20"
          variant="outlined"
          autocomplete="off"
        />
        <v-text-field
          v-model="form.author"
          label="Autore"
          :rules="[rules.required, rules.maxLen(150)]"
          counter="150"
          maxlength="150"
          variant="outlined"
          autocomplete="off"
          required
          aria-required="true"
        />

        <v-number-input
          v-model="form.totalCopies"
          label="Numero di copie"
          :min="1"
          :rules="[rules.minCopies]"
          variant="outlined"
          control-variant="stacked"
          required
          aria-required="true"
        />

        <v-select
          v-model="form.categories"
          :items="categories"
          item-title="name"
          item-value="id"
          label="Categorie"
          hint="Al massimo cinque"
          persistent-hint
          :rules="[rules.maxCategories]"
          variant="outlined"
          multiple
          chips
          closable-chips
          class="mb-4"
        />

        <v-text-field
          v-model="form.publishedAt"
          label="Data di pubblicazione"
          type="date"
          :max="today"
          :rules="[rules.notFuture]"
          variant="outlined"
        />

        <v-checkbox
          v-model="form.isHidden"
          label="Nascondi il libro agli altri utenti"
          hint="Resta visibile solo a te nella tua libreria"
          persistent-hint
        />
      </v-card-text>
    </v-card>

    <v-card variant="flat">
      <v-card-title>Immagine di copertina</v-card-title>

      <v-card-text>
        <!-- In modifica si mostra la copertina già salvata, finché non se ne sceglie una nuova. -->
        <v-img
          v-if="book?.coverImageUrl && !form.imageFile"
          :src="book.coverImageUrl"
          :alt="`Copertina attuale di ${form.title}`"
          max-width="200"
          class="mb-4 rounded"
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
import { computed, onMounted, reactive, ref, watch, useTemplateRef } from 'vue'
import { useApi } from '@/composables/useApi'

// Stesso contratto di library-form: il componente raccoglie i dati, la pagina chiama l'API.
const props = defineProps<{
  book?: any
  saving?: boolean
  submitLabel?: string
  error?: string
}>()

const emit = defineEmits<{
  submit: [payload: any, image?: File]
  cancel: []
}>()

const formRef = useTemplateRef<any>('formRef')
const api = useApi()

const MAX_IMAGE_BYTES = 5 * 1024 * 1024
const MAX_CATEGORIES = 5

// L'input type="date" e il campo del libro usano lo stesso formato yyyy-MM-dd.
const today = new Date().toISOString().slice(0, 10)

const form = reactive({
  title: props.book?.title ?? '',
  description: props.book?.description ?? '',
  author: props.book?.author ?? '',
  isbn: props.book?.isbn ?? '',
  // I libri salvati prima di questo campo hanno 0 copie: con || si riparte da 1.
  totalCopies: props.book?.totalCopies || 1,
  isHidden: props.book?.isHidden ?? false,
  // Il libro arriva con le categorie complete, la form lavora sui soli id.
  categories: props.book?.categories?.map((c: any) => c.id) ?? ([] as string[]),
  publishedAt: props.book?.publishedAt?.slice(0, 10) ?? '',
  imageFile: undefined as File | undefined,
})

// Lista fissa lato server: si carica una volta all'apertura della form.
const categories = ref<any[]>([])

onMounted(async () => {
  const response = await api.apiFetch('/categories')
  if (response.ok) {
    categories.value = await response.json()
  }
})

const imageError = ref('')
const localError = ref('')
const errorSummary = computed(() => localError.value || props.error || '')

const rules = {
  required: (v: unknown) => !!v || 'Campo obbligatorio',
  maxLen: (n: number) => (v: string) => !v || v.length <= n || `Massimo ${n} caratteri`,
  // v-number-input mette null se il campo viene svuotato
  minCopies: (v: number | null) => (v != null && v >= 1) || 'Almeno una copia',
  maxCategories: (v: string[]) =>
    v.length <= MAX_CATEGORIES || `Massimo ${MAX_CATEGORIES} categorie`,
  notFuture: (v: string) => !v || v <= today || 'La data non può essere nel futuro',
}

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
      title: form.title,
      description: form.description,
      author: form.author,
      isbn: form.isbn,
      totalCopies: form.totalCopies,
      isHidden: form.isHidden,
      categories: form.categories,
      // Campo vuoto: il server si aspetta null, non stringa vuota.
      publishedAt: form.publishedAt || null,
    },
    form.imageFile,
  )
}
</script>
