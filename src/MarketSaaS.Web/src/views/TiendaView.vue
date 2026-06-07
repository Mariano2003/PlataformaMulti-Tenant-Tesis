<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TiendaCabecera from '../components/tienda/TiendaCabecera.vue'
import TiendaEstado from '../components/tienda/TiendaEstado.vue'
import TiendaPaginacion from '../components/tienda/TiendaPaginacion.vue'
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

const buscar = ref(
  typeof route.query.q === 'string' ? route.query.q : '',
)

const pagina = ref(
  typeof route.query.pagina === 'string' && Number(route.query.pagina) > 0
    ? Number(route.query.pagina)
    : 1,
)

const { negocio, categorias, productos, total, totalPaginas, loading, error, cargar } =
  useTiendaCatalogo(slug, categoriaId, buscar, pagina)

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
  if (buscar.value.trim()) {
    return `No hay productos que coincidan con «${buscar.value.trim()}».`
  }
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

watch(
  () => route.query.q,
  (q) => {
    const next = typeof q === 'string' ? q : ''
    if (next !== buscar.value) buscar.value = next
  },
)

watch(
  () => route.query.pagina,
  (q) => {
    const n =
      typeof q === 'string' && Number(q) > 0 ? Number(q) : 1
    if (n !== pagina.value) pagina.value = n
  },
)

let debounceBuscar: ReturnType<typeof setTimeout> | null = null

function sincronizarQuery() {
  const query: Record<string, string> = {}
  if (categoriaId.value) query.categoria = categoriaId.value
  if (buscar.value.trim()) query.q = buscar.value.trim()
  if (pagina.value > 1) query.pagina = String(pagina.value)
  void router.replace({ query })
}

function seleccionarCategoria(id: string | null) {
  categoriaId.value = id
  pagina.value = 1
  sincronizarQuery()
}

function onBuscarInput() {
  if (debounceBuscar) clearTimeout(debounceBuscar)
  debounceBuscar = setTimeout(() => {
    pagina.value = 1
    sincronizarQuery()
  }, 350)
}

function paginaAnterior() {
  if (pagina.value <= 1) return
  pagina.value -= 1
  sincronizarQuery()
}

function paginaSiguiente() {
  if (pagina.value >= totalPaginas.value) return
  pagina.value += 1
  sincronizarQuery()
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

    <div v-if="!loading && !error" class="tienda-toolbar">
      <label class="tienda-buscar">
        <span class="sr-only">Buscar productos</span>
        <input
          v-model="buscar"
          type="search"
          placeholder="Buscar productos…"
          autocomplete="off"
          @input="onBuscarInput"
        />
      </label>
    </div>

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
    <TiendaPaginacion
      v-if="!loading && !error && totalPaginas > 1"
      :pagina="pagina"
      :total-paginas="totalPaginas"
      :total="total"
      :cargando="loading"
      @anterior="paginaAnterior"
      @siguiente="paginaSiguiente"
    />
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
.tienda-toolbar {
  margin: 0 0 1rem;
}
.tienda-buscar input {
  width: 100%;
  max-width: 22rem;
  padding: 0.55rem 0.85rem;
  border: 1px solid var(--border-strong, #d1d5db);
  border-radius: 10px;
  font-size: 0.95rem;
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
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  border: 0;
}
</style>
