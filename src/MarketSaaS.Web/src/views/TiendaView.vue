<script setup lang="ts">
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import TiendaCabecera from '../components/tienda/TiendaCabecera.vue'
import TiendaEstado from '../components/tienda/TiendaEstado.vue'
import ProductoCard from '../components/tienda/ProductoCard.vue'
import TiendaChatWidget from '../components/chat/TiendaChatWidget.vue'
import { useTiendaCatalogo } from '../composables/useTiendaCatalogo'
import { useRetornoPagoMercadoPago } from '../composables/useRetornoPagoMercadoPago'
import { useCarritoStore } from '../stores/carrito'

const route = useRoute()
const slug = computed(() => (route.params.slug as string) || '')
const { negocio, productos, loading, error, cargar } = useTiendaCatalogo(slug)
const { retornoPago, confirmandoPago } = useRetornoPagoMercadoPago(() => slug.value, {
  onDespuesConfirmar: () => cargar(),
})

const carrito = useCarritoStore()
watch(
  slug,
  (s) => {
    carrito.setTienda(s)
  },
  { immediate: true },
)


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
  <div class="tienda-page">
    <TiendaCabecera
      :negocio="negocio"
      :slug="slug"
      :total-carrito="carrito.totalUnidades"
    />

    <p
      v-if="retornoPago"
      class="pago-retorno"
      :class="{
        'pago-retorno--ok': retornoPago.tipo === 'ok',
        'pago-retorno--error': retornoPago.tipo === 'error',
        'pago-retorno--pending': retornoPago.tipo === 'pending',
      }"
      role="status"
    >
      {{ retornoPago.texto }}
      <span v-if="confirmandoPago"> (sincronizando…)</span>
    </p>

    <TiendaEstado
      :loading="loading"
      :error="error"
      :sin-productos="sinProductos"
    />
    <ul v-if="!loading && !error && productos.length" class="producto-grid">
      <li v-for="p in productos" :key="p.id">
        <ProductoCard :producto="p" />
      </li>
    </ul>
    <TiendaChatWidget v-if="slug" :slug="slug" />
  </div>
</template>

<style scoped>
.pago-retorno {
  margin: 0 0 1rem;
  padding: 0.85rem 1rem;
  border-radius: 10px;
  font-size: 0.95rem;
  line-height: 1.45;
}
.pago-retorno--ok {
  background: #ecfdf5;
  color: #065f46;
  border: 1px solid #a7f3d0;
}
.pago-retorno--error {
  background: #fef2f2;
  color: #991b1b;
  border: 1px solid #fecaca;
}
.pago-retorno--pending {
  background: #fffbeb;
  color: #92400e;
  border: 1px solid #fde68a;
}
</style>
