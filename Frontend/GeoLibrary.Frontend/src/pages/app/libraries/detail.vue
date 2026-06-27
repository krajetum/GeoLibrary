<template>
  <v-container fluid>
    <v-row>
      <v-col> </v-col>
    </v-row>

    <v-row>
      <v-col>
        <v-datatable></v-datatable>
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

onMounted(() => {
  const id = route.params.id
  // TODO: backend
})
</script>
