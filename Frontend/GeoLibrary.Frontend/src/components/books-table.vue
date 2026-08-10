<template>
  <div>
    <v-text-field
      v-model="searchInput"
      label="Cerca per titolo, autore o ISBN"
      prepend-inner-icon="mdi-magnify"
      variant="outlined"
      density="comfortable"
      clearable
      hide-details
      class="mb-4"
    />

    <v-data-table-server
      v-model:page="page"
      v-model:items-per-page="itemsPerPage"
      v-model:sort-by="sortBy"
      :headers="headers"
      :items="books"
      :items-length="totalItems"
      :loading="loading"
      :search="search"
      item-value="id"
      hover
      @update:options="loadItems"
      @click:row="goToBookDetail"
    >
      <template #item.cover="{ item }">
        <v-img
          v-if="item.coverThumbnailUrl"
          :src="item.coverThumbnailUrl"
          :alt="`Copertina di ${item.title}`"
          width="40"
          height="56"
          cover
          class="my-1 rounded"
        />
        <v-icon v-else icon="mdi-book-outline" class="text-medium-emphasis" />
      </template>

      <template #item.title="{ item }">
        {{ item.title }}
        <v-icon
          v-if="item.isHidden"
          icon="mdi-eye-off-outline"
          size="small"
          class="ms-1 text-medium-emphasis"
          aria-label="Nascosto agli altri utenti"
        />
      </template>

      <template #no-data>
        <span class="text-medium-emphasis">Nessun libro trovato.</span>
      </template>
    </v-data-table-server>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useDebounceFn } from '@vueuse/core'
import { useApi } from '@/composables/useApi'
import { useAppLink } from '@/composables/useAppLink'
import { useRouter } from 'vue-router'

const router = useRouter()
const { bookPath } = useAppLink()

const props = defineProps<{ libraryId: string }>()

const { apiFetch } = useApi()

const books = ref<any[]>([])
const totalItems = ref(0)
const loading = ref(false)

const page = ref(1)
const itemsPerPage = ref(10)
const sortBy = ref<{ key: string; order: 'asc' | 'desc' }[]>([{ key: 'title', order: 'asc' }])

// searchInput è ciò che l'utente digita; search (debounced) è ciò che filtra.
const searchInput = ref('')
const search = ref('')
const applySearch = useDebounceFn((value: string) => {
  search.value = value
}, 400)
watch(searchInput, (value) => applySearch(value))

// per avere il typecheck, purtroppo vuetify non espone il tipo
interface DataTableOptions {
  page: number
  itemsPerPage: number
  sortBy: { key: string; order: 'asc' | 'desc' }[]
  search?: string
}

const headers = [
  { title: 'Copertina', key: 'cover', sortable: false, width: 80 },
  { title: 'Titolo', key: 'title' },
  { title: 'Autore', key: 'author' },
  { title: 'ISBN', key: 'isbn' },
]

async function loadItems(options: DataTableOptions) {
  loading.value = true
  try {
    const params = new URLSearchParams({
      page: String(options.page),
      itemsPerPage: String(options.itemsPerPage),
    })

    const sort = options.sortBy?.[0]
    if (sort) {
      params.set('sortBy', sort.key)
      params.set('sortDesc', String(sort.order === 'desc'))
    }
    if (options.search) {
      params.set('search', options.search)
    }

    const res = await apiFetch(`/library/${props.libraryId}/books?${params.toString()}`)
    if (!res.ok) throw new Error(`HTTP ${res.status}`)

    const data = await res.json()
    books.value = data.items
    totalItems.value = data.totalCount
  } catch {
    books.value = []
    totalItems.value = 0
  } finally {
    loading.value = false
  }
}

function goToBookDetail(event: any, book: any) {
  router.push(bookPath(props.libraryId, book.item.id))
}
</script>
