<template>
  <v-container class="py-8">
    <h1 class="text-h4 mb-6">Aggiungi un nuovo libro</h1>

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
            :rules="[rules.maxLen(150)]"
            counter="150"
            maxlength="150"
            variant="outlined"
            autocomplete="off"
            required
            aria-required="true"
            autofocus
          />
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
        </v-card-text>
      </v-card>

      <!-- Azioni: bottone primario chiaro + annulla. Stato di loading bloccante. -->
      <div class="d-flex justify-end ga-3">
        <v-btn variant="text" :disabled="saving" @click="onCancel"> Annulla </v-btn>
        <v-btn type="submit" color="primary" :loading="saving" prepend-icon="mdi-content-save">
          Salva
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
import { reactive, ref, watch, useTemplateRef } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useApi } from '@/composables/useApi'
import { useGeocoding } from '@/composables/useGeocoding'

const route = useRoute()
const router = useRouter()
const { apiFetch } = useApi()
const libraryId = route.params.id as string

const formRef = useTemplateRef<any>('formRef')
const errorAlert = useTemplateRef<any>('errorAlert')

const form = reactive({
  title: '',
  description: '',
  author: '',
  isbn: '',
  imageFile: undefined as File | undefined,
})

const saving = ref(false)
const imageError = ref('')
const errorSummary = ref('')
const snackbar = reactive({ show: false, text: '', color: 'success' })

const rules = {
  required: (v: unknown) => !!v || 'Campo obbligatorio',
  maxLen: (n: number) => (v: string) => !v || v.length <= n || `Massimo ${n} caratteri`,
}

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
      libraryId,
      title: form.title,
      description: form.description,
      author: form.author,
      isbn: form.isbn,
    }

    const res = await apiFetch('/book', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)

    snackbar.text = 'Libro aggiunto con successo.'
    snackbar.color = 'success'
    snackbar.show = true
    router.push(`/app/libraries/${libraryId}`)
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
