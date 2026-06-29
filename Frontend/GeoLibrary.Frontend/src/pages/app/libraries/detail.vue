<template>
  <v-container>
    <v-row>
      <v-col>
        <v-card variant="flat">
          <v-img src="/placeholder_library.jpg" cover max-height="350px"> </v-img>
          <v-card-title>
            {{ library.name }}
          </v-card-title>
          <v-card-subtitle>
            {{ library.address }}
            <v-btn @click="goToMaps()" size="small" icon text>
              <v-icon>mdi-map-marker</v-icon>
            </v-btn>
          </v-card-subtitle>
        </v-card>
      </v-col>
    </v-row>

    <v-row>
      <v-col>
        <v-data-table
          :items-per-page="itemsPerPage"
          :headers="headers"
          :items="books"
          :loading="loadingBooks"
        ></v-data-table>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useApi } from '@/composables/useApi'

const api = useApi()
const route = useRoute()

const library = ref<any>({})
const books = ref<any[]>([])
const headers = ref([
  {
    title: 'Title',
    key: 'title',
  },
  {
    title: 'Author',
    key: 'author',
  },
])

const loadingBooks = ref(true)
const totalItems = ref(0)
const itemsPerPage = ref(5)
onMounted(async () => {
  const id = route.params.id
  var response = await api.apiFetch(`/library/${id}`)

  if (response.status !== 200) {
    // TODO: error
  }

  library.value = await response.json()

  var booksResponse = await api.apiFetch(`/library/${id}/books`)
  if (booksResponse.status !== 200) {
    // TODO: error
  }
  loadingBooks.value = false
  books.value = await booksResponse.json()
})

/*
Source - https://stackoverflow.com/a/6240537
Posted by Yilmaz Guleryuz, modified by community. See post 'Timeline' for change history
Retrieved 2026-06-29, License - CC BY-SA 3.0
*/
function goToMaps() {
  window.open(
    `https://maps.google.com/?q=${library.value.latitude},${library.value.longitude}`,
    '_blank',
  )
}
</script>
