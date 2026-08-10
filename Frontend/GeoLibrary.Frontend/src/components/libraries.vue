<template>
  <v-container fluid>
    <v-row>
      <v-col>
        <h2>My libraries</h2>
      </v-col>
    </v-row>
    <v-row>
      <v-col cols="3" v-for="library in libraries" :key="library.id">
        <v-card link hover>
          <v-img
            :alt="library.name"
            @click="goToLibrary(library.id)"
            :src="library.thumbnailUrl ?? '/placeholder_library.jpg'"
            class="align-end"
            gradient="to bottom, rgba(0,0,0,.1), rgba(0,0,0,.5)"
            height="300px"
            cover
          >
            <v-card-title class="text-white">
              {{ library.name }}

              <v-chip prepend-icon="mdi-book-open-variant" variant="outlined"
                >{{ library.bookCount }}
              </v-chip>
            </v-card-title>
            <v-card-subtitle class="text-white">
              {{ library.address }}
            </v-card-subtitle>
          </v-img>
        </v-card>
      </v-col>
      <v-col cols="3" v-if="isAddEnabled">
        <v-card variant="outlined" class="add-card" link to="/app/libraries/new" hovers>
          <div class="add-cta">
            <v-icon size="48" color="white">mdi-plus</v-icon>
          </div>
        </v-card>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAppLink } from '@/composables/useAppLink'

const router = useRouter()
const { libraryPath } = useAppLink()

const props = defineProps<{
  isAddEnabled?: boolean
  libraries?: Array<any>
}>()

function goToLibrary(id: string) {
  router.push(libraryPath(id))
}
</script>

<style>
.add-card {
  height: 100%;
  width: 100%;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #88e788;
  border-style: dashed;
  border-width: 4px;
  border-color: #58c358;
}

.add-card .add-cta {
  display: flex;
  align-items: center;
  flex-direction: column;
}
.add-card .add-cta div {
  font-size: 2rem;
}
</style>
