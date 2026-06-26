<template>
  <div class="w-full bg-white shadow-md rounded-lg p-4">
    <div class="flex">
      <button
        :class="[
          'px-4 py-2 cursor-pointer',
          activeTab === 'address'
            ? 'bg-blue-500 text-white hover:bg-blue-600'
            : 'bg-gray-200 text-gray-700 hover:bg-gray-300',
        ]"
        @click="activeTab = 'address'"
      >
        Address
      </button>
      <button
        :class="[
          'px-4 py-2 cursor-pointer',
          activeTab === 'map'
            ? 'bg-blue-500 text-white hover:bg-blue-600'
            : 'bg-gray-200 text-gray-700 hover:bg-gray-300',
        ]"
        @click="activeTab = 'map'"
      >
        Map
      </button>
      <button
        :class="[
          'px-4 py-2 cursor-pointer',
          activeTab === 'bookName'
            ? 'bg-blue-500 text-white hover:bg-blue-600'
            : 'bg-gray-200 text-gray-700 hover:bg-gray-300',
        ]"
        @click="activeTab = 'bookName'"
      >
        Book Name
      </button>
    </div>
    <div class="p-4 border-1">
      <div v-if="activeTab === 'address'" class="mt-4">
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Enter address..."
          class="w-full px-4 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <div v-if="activeTab === 'map'" class="mt-4">
        <div style="height: 600px">
          <div v-if="isLoadingMap" class="flex items-center justify-center h-full">
            <p>Loading map...</p>
          </div>
          <div v-else-if="!isSupported" class="flex items-center justify-center h-full">
            <p>Geolocation is not supported by this browser.</p>
          </div>
          <div v-else-if="error" class="flex items-center justify-center h-full">
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
      </div>
      <div v-if="activeTab === 'bookName'" class="mt-4">
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Enter book name..."
          class="w-full px-4 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
    </div>
  </div>
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
