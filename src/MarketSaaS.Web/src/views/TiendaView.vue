<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import TiendaCabecera from '../components/tienda/TiendaCabecera.vue'
import TiendaEstado from '../components/tienda/TiendaEstado.vue'
import ProductoCard from '../components/tienda/ProductoCard.vue'
import TiendaChatWidget from '../components/chat/TiendaChatWidget.vue'
import { useTiendaCatalogo } from '../composables/useTiendaCatalogo'
import { useCarritoStore } from '../stores/carrito'

const route = useRoute()
const slug = computed(() => (route.params.slug as string) || '')

const carrito = useCarritoStore()
watch(
  slug,
  (s) => {
    carrito.setTienda(s)
  },
  { immediate: true },
)

const { negocio, productos, loading, error } = useTiendaCatalogo(slug)

const sinProductos = computed(
  () => !loading.value && !error.value && productos.value.length === 0,
)

watch(
  [productos, loading, error, slug],
  () => {
    if (loading.value || error.value) return
    const s = slug.value.trim()
    if (!s || carrito.slugTienda !== s) return
    carrito.syncLineasConCatalogo(productos.value)
  },
  { deep: true },
)
</script>

<template>
  <div class="tienda">
    <TiendaCabecera
      :negocio="negocio"
      :slug="slug"
      :total-carrito="carrito.totalUnidades"
    />
    <TiendaEstado
      :loading="loading"
      :error="error"
      :sin-productos="sinProductos"
    />
    <ul v-if="!loading && !error && productos.length" class="grid">
      <li v-for="p in productos" :key="p.id" class="celda">
        <ProductoCard :producto="p" />
      </li>
    </ul>
    <TiendaChatWidget v-if="slug" :slug="slug" />
  </div>
</template>

<style scoped>
.tienda {
  padding: 1.5rem 1.25rem 3rem;
  max-width: 56rem;
  margin: 0 auto;
  text-align: left;
}
.grid {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(auto-fill, minmax(15rem, 1fr));
}
.celda {
  margin: 0;
}
</style>
