<template>
  <v-container style="max-width: 720px">
    <h1 class="text-h5 font-weight-bold mb-4">Il tuo profilo</h1>

    <!-- Dati personali -->
    <v-card variant="flat" class="rounded-lg mb-6">
      <v-card-title class="text-h6">Dati personali</v-card-title>
      <v-card-text>
        <v-form ref="formRef" validate-on="submit" @submit.prevent="onSaveProfile">
          <v-text-field
            v-model="displayName"
            label="Nome visualizzato"
            hint="È il nome che vedono gli altri utenti sulle tue librerie e sulle richieste di prestito"
            persistent-hint
            :rules="[rules.required, rules.maxLen(100)]"
            counter="100"
            maxlength="100"
            variant="outlined"
            required
            aria-required="true"
          />

          <!-- L'email arriva da Keycloak: si modifica di là, non da qui. -->
          <v-text-field
            :model-value="auth.userInfo.email ?? ''"
            label="Email"
            variant="outlined"
            readonly
            class="mt-4"
            hint="Gestita dal servizio di autenticazione, non modificabile da questa pagina"
            persistent-hint
          />

          <div class="d-flex justify-end mt-4">
            <v-btn
              type="submit"
              color="primary"
              :loading="savingProfile"
              prepend-icon="mdi-content-save"
            >
              Salva
            </v-btn>
          </div>
        </v-form>
      </v-card-text>
    </v-card>

    <!-- Avatar -->
    <v-card variant="flat" class="rounded-lg mb-6">
      <v-card-title class="text-h6">Immagine di profilo</v-card-title>
      <v-card-text>
        <div class="d-flex align-center ga-4 mb-4">
          <v-avatar size="80">
            <v-img
              :src="avatarPreview"
              :alt="`Immagine di profilo di ${displayName || 'utente'}`"
            />
          </v-avatar>
          <p class="text-body-2 text-medium-emphasis mb-0">
            Un'immagine quadrata rende meglio. Ne viene generata automaticamente una miniatura.
          </p>
        </div>

        <!-- Stessa dropzone usata per le copertine, con gli stessi limiti -->
        <v-file-upload
          v-model="avatarFile"
          accept="image/jpeg,image/png"
          icon="mdi-account-box-outline"
          title="Trascina un'immagine o premi per selezionarla"
          subtitle="JPG o PNG, max 5 MB"
          density="comfortable"
          clearable
        />

        <p v-if="avatarError" class="text-error text-caption mt-2" role="alert">
          {{ avatarError }}
        </p>

        <div class="d-flex justify-end mt-4">
          <v-btn
            color="primary"
            :disabled="!avatarFile"
            :loading="uploadingAvatar"
            prepend-icon="mdi-upload"
            @click="onUploadAvatar"
          >
            Carica immagine
          </v-btn>
        </div>
      </v-card-text>
    </v-card>

    <!-- Eliminazione account -->
    <v-card variant="outlined" class="rounded-lg border-error">
      <v-card-title class="text-h6">Elimina il tuo account</v-card-title>
      <v-card-text>
        <p class="mb-2">L'operazione è definitiva e cancella:</p>
        <ul class="ms-6 mb-4">
          <li>il tuo profilo e l'immagine di profilo;</li>
          <li>tutte le tue librerie e i libri che contengono;</li>
          <li>le immagini di copertina caricate.</li>
        </ul>
        <p class="text-caption text-medium-emphasis">
          Le credenziali di accesso restano sul servizio di autenticazione e vanno rimosse
          separatamente.
        </p>
        <div class="d-flex justify-end">
          <v-btn color="error" variant="outlined" @click="confirmDelete = true">
            Elimina account
          </v-btn>
        </div>
      </v-card-text>
    </v-card>

    <v-dialog v-model="confirmDelete" max-width="460">
      <v-card>
        <v-card-title>Eliminare l'account?</v-card-title>
        <v-card-text>
          Questa operazione è irreversibile: profilo, librerie, libri e immagini verranno cancellati
          definitivamente.
        </v-card-text>
        <v-card-actions class="justify-end">
          <v-btn variant="text" :disabled="deleting" @click="confirmDelete = false">Annulla</v-btn>
          <v-btn color="error" :loading="deleting" @click="onDeleteAccount">Elimina</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-snackbar v-model="snackbar.show" :color="snackbar.color" role="status">
      {{ snackbar.text }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, useTemplateRef } from 'vue'
import { useApi } from '@/composables/useApi'
import { useAuthStore } from '@/stores/auth'

const api = useApi()
const auth = useAuthStore()

const MAX_AVATAR_BYTES = 5 * 1024 * 1024

const formRef = useTemplateRef<any>('formRef')

const displayName = ref('')
// v-file-upload lavora con undefined per "nessun file", non con null.
const avatarFile = ref<File | undefined>()
const avatarError = ref('')

const savingProfile = ref(false)
const uploadingAvatar = ref(false)
const deleting = ref(false)
const confirmDelete = ref(false)

const snackbar = reactive({ show: false, text: '', color: 'success' })

const rules = {
  required: (v: string) => !!v?.trim() || 'Campo obbligatorio',
  maxLen: (max: number) => (v: string) => !v || v.length <= max || `Massimo ${max} caratteri`,
}

/** Finché non si carica nulla si mostra l'avatar salvato, altrimenti il segnaposto. */
const avatarPreview = computed(() => auth.userInfo.avatarUrl ?? '/avatar.png')

// Il profilo è già nello store: qui serve solo la copia modificabile del campo.
onMounted(() => {
  displayName.value = auth.userInfo.displayName ?? ''
})

function notify(text: string, color: 'success' | 'error' = 'success') {
  snackbar.text = text
  snackbar.color = color
  snackbar.show = true
}

async function onSaveProfile() {
  const validation = await formRef.value?.validate()
  if (validation && !validation.valid) return

  savingProfile.value = true
  try {
    const response = await api.apiFetch('/user/profile/me', {
      method: 'PATCH',
      body: JSON.stringify({ displayName: displayName.value.trim() }),
    })

    if (!response.ok) {
      notify('Salvataggio non riuscito.', 'error')
      return
    }

    // Riallinea la barra in alto senza ricaricare la pagina.
    await auth.fetchProfile()
    notify('Profilo aggiornato.')
  } catch (e) {
    console.error('Errore nel salvataggio del profilo:', e)
    notify('Salvataggio non riuscito.', 'error')
  } finally {
    savingProfile.value = false
  }
}

async function onUploadAvatar() {
  avatarError.value = ''
  const file = avatarFile.value
  if (!file) return

  // Controllo lato client per dare un errore immediato; il server ripete comunque il suo.
  if (file.size > MAX_AVATAR_BYTES) {
    avatarError.value = "L'immagine non può superare i 5 MB."
    return
  }

  uploadingAvatar.value = true
  try {
    const body = new FormData()
    // Il nome del campo deve combaciare con il parametro IFormFile avatar del controller.
    body.append('avatar', file)

    const response = await api.apiFetch('/user/profile/me/avatar', { method: 'PATCH', body })

    if (!response.ok) {
      avatarError.value = 'Caricamento non riuscito.'
      return
    }

    avatarFile.value = undefined
    await auth.fetchProfile()
    notify('Immagine di profilo aggiornata.')
  } catch (e) {
    console.error("Errore nel caricamento dell'avatar:", e)
    avatarError.value = 'Caricamento non riuscito.'
  } finally {
    uploadingAvatar.value = false
  }
}

async function onDeleteAccount() {
  deleting.value = true
  try {
    const response = await api.apiFetch('/user/profile/me', { method: 'DELETE' })
    if (!response.ok) {
      notify('Eliminazione non riuscita.', 'error')
      return
    }

    // Senza account non c'è più niente da mostrare nell'area riservata.
    auth.logout()
  } catch (e) {
    console.error("Errore nell'eliminazione dell'account:", e)
    notify('Eliminazione non riuscita.', 'error')
  } finally {
    deleting.value = false
    confirmDelete.value = false
  }
}
</script>
