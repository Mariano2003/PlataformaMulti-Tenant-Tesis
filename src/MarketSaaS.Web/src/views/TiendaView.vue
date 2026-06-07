<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TiendaCabecera from '../components/tienda/TiendaCabecera.vue'
import TiendaEstado from '../components/tienda/TiendaEstado.vue'
import ProductoCard from '../components/tienda/ProductoCard.vue'
import TiendaChatWidget from '../components/chat/TiendaChatWidget.vue'
import { useTiendaCatalogo } from '../composables/useTiendaCatalogo'
import { useRetornoPagoMercadoPago } from '../composables/useRetornoPagoMercadoPago'
import { useCarritoStore } from '../stores/carrito'

const route = useRoute()
const router = useRouter()
const slug = computed(() => (route.params.slug as string) || '')

const categoriaId = ref<string | null>(
  typeof route.query.categoria === 'string' && route.query.categoria.trim()
    ? route.query.categoria.trim()
    : null,
)

const { negocio, categorias, productos, loading, error, cargar } = useTiendaCatalogo(
  slug,
  categoriaId,
)
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

const mensajeSinProductos = computed(() => {
  if (categoriaId.value) {
    const cat = categorias.value.find((c) => c.id === categoriaId.value)
    const nombre = cat?.nombre ?? 'esta categoría'
    return `No hay productos en «${nombre}».`
  }
  return 'No hay productos activos en esta tienda.'
})

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

watch(
  () => route.query.categoria,
  (q) => {
    const next =
      typeof q === 'string' && q.trim() ? q.trim() : null
    if (next !== categoriaId.value) categoriaId.value = next
  },
)

function seleccionarCategoria(id: string | null) {
  categoriaId.value = id
  const query = { ...route.query }
  if (id) query.categoria = id
  else delete query.categoria
  void router.replace({ query })
}
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

    <nav
      v-if="!loading && !error && categorias.length"
      class="tienda-filtros"
      aria-label="Filtrar por categoría"
    >
      <button
        type="button"
        class="tienda-filtro"
        :class="{ 'tienda-filtro--activo': !categoriaId }"
        @click="seleccionarCategoria(null)"
      >
        Todas
      </button>
      <button
        v-for="c in categorias"
        :key="c.id"
        type="button"
        class="tienda-filtro"
        :class="{ 'tienda-filtro--activo': categoriaId === c.id }"
        @click="seleccionarCategoria(c.id)"
      >
        {{ c.nombre }}
      </button>
    </nav>

    <TiendaEstado
      :loading="loading"
      :error="error"
      :sin-productos="sinProductos"
      :mensaje-sin-productos="mensajeSinProductos"
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
.tienda-filtros {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin: 0 0 1.25rem;
}
.tienda-filtro {
  padding: 0.4rem 0.85rem;
  border: 1px solid var(--border-strong, #d1d5db);
  border-radius: 999px;
  background: var(--surface, #fff);
  color: var(--text, #374151);
  font-size: 0.9rem;
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s, color 0.15s;
}
.tienda-filtro:hover {
  border-color: var(--accent, #2563eb);
  color: var(--accent, #2563eb);
}
.tienda-filtro--activo {
  background: var(--accent, #2563eb);
  border-color: var(--accent, #2563eb);
  color: #fff;
}
</style>
