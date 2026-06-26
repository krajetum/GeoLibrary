<template>
  <v-card>
    <v-tabs v-model="activeTab" color="primary">
      <v-tab value="address">Address</v-tab>
      <v-tab value="map">Map</v-tab>
      <v-tab value="bookName">Book Name</v-tab>
    </v-tabs>

    <v-card-text>
      <v-tabs-window v-model="activeTab">
        <v-tabs-window-item value="address">
          <v-text-field
            v-model="searchQuery"
            label="Enter address..."
            variant="outlined"
            hide-details
          />
        </v-tabs-window-item>

        <v-tabs-window-item value="map">
          <div style="height: 600px">
            <div v-if="isLoadingMap" class="d-flex align-center justify-center h-100">
              <p>Loading map...</p>
            </div>
            <div v-else-if="!isSupported" class="d-flex align-center justify-center h-100">
              <p>Geolocation is not supported by this browser.</p>
            </div>
            <div v-else-if="error" class="d-flex align-center justify-center h-100">
              <p>Error getting geolocation: {{ error.message }}</p>
            </div>
            <l-map v-else ref="map" v-model:zoom="zoom" :center="center">
              <l-tile-layer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                layer-type="base"
                name="OpenStreetMap"
              ></l-tile-layer>
              <l-circle-marker
                v-if="center[0] !== 0 && center[1] !== 0"
                :lat-lng="center"
                :radius="10"
                color="blue"
              ></l-circle-marker>
            </l-map>
          </div>
        </v-tabs-window-item>

        <v-tabs-window-item value="bookName">
          <v-text-field
            v-model="searchQuery"
            label="Enter book name..."
            variant="outlined"
            hide-details
          />
        </v-tabs-window-item>
      </v-tabs-window>
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import 'leaflet/dist/leaflet.css'
import { ref, watch } from 'vue'
import { LMap, LTileLayer, LCircleMarker } from '@vue-leaflet/vue-leaflet'
import { useGeolocation } from '@vueuse/core'

const { coords, isSupported, error } = useGeolocation()

const activeTab = ref('address')
const searchQuery = ref('')

const zoom = ref(2)
const center = ref<[number, number]>([0, 0])
const isLoadingMap = ref(true)

const stop = watch(
  coords,
  (c) => {
    if (!isSupported.value) {
      console.error('Geolocation is not supported by this browser.')
      isLoadingMap.value = false
      stop()
      return
    }

    if (error.value) {
      console.error('Error getting geolocation:', error.value)
      isLoadingMap.value = false
      stop()
      return
    }

    if (Number.isFinite(c.latitude) && (c.latitude !== 0 || c.longitude !== 0)) {
      isLoadingMap.value = false
      center.value = [c.latitude, c.longitude]
      zoom.value = 13
      stop()
    }
  },
  { immediate: true },
)
</script>
