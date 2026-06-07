<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { useAuthedFetch } from '../composables/useAuthedFetch'
import { useAuthStore } from '../stores/auth'
import type { PedidoClienteListItemDto } from '../types/api'
import {
  PASOS_SEGUIMIENTO_PEDIDO,
  claseEstadoPedidoCliente,
  etiquetaEstadoPedido,
  indiceSeguimientoPedido,
  pedidoDebeAutoActualizar,
  pedidoMuestraSeguimiento,
} from '../types/api'

const auth = useAuthStore()
const authedFetch = useAuthedFetch()

const pedidos = ref<PedidoClienteListItemDto[]>([])
const cargando = ref(true)
const refrescando = ref(false)
const error = ref<string | null>(null)
const expandidoId = ref<string | null>(null)
const ultimaActualizacion = ref<Date | null>(null)

const precioFmt = new Intl.NumberFormat('es-AR', {
  style: 'currency',
  currency: 'ARS',
  maximumFractionDigits: 2,
})

const hayPedidosActivos = computed(() =>
  pedidos.value.some((p) => pedidoDebeAutoActualizar(p.estado)),
)

const textoUltimaActualizacion = computed(() => {
  if (!ultimaActualizacion.value) return null
  return ultimaActualizacion.value.toLocaleTimeString('es-AR', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
})

function clasePill(estado: string) {
  const c = claseEstadoPedidoCliente(estado)
  if (c === 'ok') return 'mis-pedidos__pill--ok'
  if (c === 'err') return 'mis-pedidos__pill--err'
  if (c === 'pending') return 'mis-pedidos__pill--pending'
  if (c === 'progress') return 'mis-pedidos__pill--progress'
  return ''
}

function formatearFecha(iso: string) {
  try {
    return new Date(iso).toLocaleString('es-AR', {
      dateStyle: 'short',
      timeStyle: 'short',
    })
  } catch {
    return iso
  }
}

function toggleDetalle(id: string) {
  expandidoId.value = expandidoId.value === id ? null : id
}

function pasoCompletado(pasoIdx: number, estado: string) {
  const actual = indiceSeguimientoPedido(estado)
  return actual >= pasoIdx
}

function pasoActual(pasoIdx: number, estado: string) {
  return indiceSeguimientoPedido(estado) === pasoIdx
}

async function cargar(silencioso = false) {
  if (silencioso) refrescando.value = true
  else {
    cargando.value = true
    pedidos.value = []
  }
  error.value = null
  try {
    const res = await authedFetch('/api/mis-pedidos?limite=50')
    if (res.status === 401) {
      auth.cerrarSesion()
      return
    }
    if (!res.ok) {
      if (!silencioso) {
        error.value = `No se pudieron cargar tus pedidos (${res.status}).`
      }
      return
    }
    const raw = (await res.json()) as unknown
    pedidos.value = Array.isArray(raw) ? (raw as PedidoClienteListItemDto[]) : []
    ultimaActualizacion.value = new Date()
  } catch {
    if (!silencioso) error.value = 'No se pudo conectar con la API.'
  } finally {
    cargando.value = false
    refrescando.value = false
  }
}

let intervalo: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  void cargar()
  intervalo = setInterval(() => {
    if (hayPedidosActivos.value) void cargar(true)
  }, 12_000)
})

onUnmounted(() => {
  if (intervalo) clearInterval(intervalo)
})
</script>

<template>
  <main class="mis-pedidos-page">
    <header class="mis-pedidos__head">
      <div>
        <h1>Mis pedidos</h1>
        <p class="mis-pedidos__sub">
          Compras asociadas a <strong>{{ auth.usuario?.email }}</strong>
        </p>
        <p v-if="textoUltimaActualizacion && !cargando" class="mis-pedidos__sync">
          <span v-if="refrescando">Actualizando estado…</span>
          <span v-else-if="hayPedidosActivos">
            Se actualiza solo · última consulta {{ textoUltimaActualizacion }}
          </span>
          <span v-else>Última consulta {{ textoUltimaActualizacion }}</span>
        </p>
      </div>
      <div class="mis-pedidos__actions">
        <button type="button" class="mis-pedidos__btn-ghost" :disabled="cargando" @click="() => void cargar()">
          Actualizar
        </button>
        <RouterLink class="mis-pedidos__btn-ghost" :to="{ name: 'tiendas' }">Mis tiendas</RouterLink>
        <button type="button" class="mis-pedidos__btn-ghost" @click="auth.cerrarSesion()">Salir</button>
      </div>
    </header>

    <p v-if="cargando" class="mis-pedidos__state">Cargando pedidos…</p>
    <p v-else-if="error" class="mis-pedidos__state mis-pedidos__state--err">{{ error }}</p>
    <p v-else-if="!pedidos.length" class="mis-pedidos__state">
      Todavía no tenés pedidos. Elegí una
      <RouterLink :to="{ name: 'tiendas' }">tienda</RouterLink>
      y comprá con el mismo email de tu cuenta.
    </p>

    <ul v-else class="mis-pedidos__list">
      <li v-for="p in pedidos" :key="p.id" class="mis-pedidos__card">
        <div class="mis-pedidos__card-head">
          <div>
            <RouterLink
              v-if="p.negocioSlug"
              class="mis-pedidos__tienda"
              :to="{ name: 'tienda', params: { slug: p.negocioSlug } }"
            >
              {{ p.negocioNombre }}
            </RouterLink>
            <span v-else class="mis-pedidos__tienda">{{ p.negocioNombre }}</span>
            <p class="mis-pedidos__fecha">{{ formatearFecha(p.creadoEn) }}</p>
          </div>
          <div class="mis-pedidos__card-meta">
            <span class="mis-pedidos__pill" :class="clasePill(p.estado)">
              {{ etiquetaEstadoPedido(p.estado) }}
            </span>
            <span class="mis-pedidos__total">{{ precioFmt.format(p.total) }}</span>
          </div>
        </div>

        <ol
          v-if="pedidoMuestraSeguimiento(p.estado)"
          class="mis-pedidos__seguimiento"
          aria-label="Seguimiento del pedido"
        >
          <li
            v-for="(paso, idx) in PASOS_SEGUIMIENTO_PEDIDO"
            :key="paso.clave"
            class="mis-pedidos__paso"
            :class="{
              'mis-pedidos__paso--hecho': pasoCompletado(idx, p.estado),
              'mis-pedidos__paso--actual': pasoActual(idx, p.estado),
            }"
          >
            <span class="mis-pedidos__paso-dot" aria-hidden="true" />
            <span class="mis-pedidos__paso-label">{{ paso.etiqueta }}</span>
          </li>
        </ol>

        <p v-else-if="p.estado === 'PendientePago' || p.estado === 'ProcesandoPago'" class="mis-pedidos__hint">
          Esperando confirmación del pago. Esta pantalla se actualiza sola.
        </p>
        <p v-else-if="p.estado === 'Rechazado'" class="mis-pedidos__hint mis-pedidos__hint--err">
          El pago no se completó. Podés volver a la tienda e intentar de nuevo.
        </p>
        <p v-else-if="p.estado === 'Cancelado'" class="mis-pedidos__hint mis-pedidos__hint--err">
          Este pedido fue cancelado por la tienda.
        </p>

        <button type="button" class="mis-pedidos__toggle" @click="toggleDetalle(p.id)">
          {{ expandidoId === p.id ? 'Ocultar detalle' : 'Ver productos' }}
        </button>

        <ul v-if="expandidoId === p.id" class="mis-pedidos__lineas">
          <li v-for="l in p.lineas" :key="l.productoId + l.nombre">
            <span>{{ l.nombre }}</span>
            <span>{{ l.cantidad }} × {{ precioFmt.format(l.precioUnitario) }}</span>
            <span>{{ precioFmt.format(l.subtotal) }}</span>
          </li>
        </ul>
      </li>
    </ul>
  </main>
</template>

<style scoped>
.mis-pedidos-page {
  padding: 1.5rem clamp(1rem, 4vw, 2rem) 3rem;
  max-width: 48rem;
  margin: 0 auto;
}
.mis-pedidos__head {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  margin-bottom: 1.5rem;
}
.mis-pedidos__head h1 {
  margin: 0 0 0.35rem;
  font-size: 1.75rem;
}
.mis-pedidos__sub {
  margin: 0;
  color: var(--text);
  font-size: 0.95rem;
}
.mis-pedidos__sync {
  margin: 0.35rem 0 0;
  font-size: 0.8rem;
  color: var(--text-muted);
}
.mis-pedidos__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}
.mis-pedidos__btn-ghost {
  padding: 0.4rem 0.75rem;
  border-radius: 999px;
  border: 1px solid var(--border-strong);
  background: var(--surface);
  color: var(--primary-dark);
  font-size: 0.88rem;
  font-weight: 600;
  text-decoration: none;
  cursor: pointer;
}
.mis-pedidos__btn-ghost:hover {
  border-color: var(--primary);
}
.mis-pedidos__btn-ghost:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.mis-pedidos__state {
  margin: 0;
  color: var(--text-muted);
}
.mis-pedidos__state--err {
  color: #b91c1c;
}
.mis-pedidos__state a {
  color: var(--primary-dark);
  font-weight: 600;
}
.mis-pedidos__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.mis-pedidos__card {
  border-radius: var(--radius-md);
  border: 1px solid var(--border);
  background: var(--surface);
  box-shadow: var(--shadow-sm);
  padding: 1rem 1.1rem;
}
.mis-pedidos__card-head {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 0.75rem;
}
.mis-pedidos__tienda {
  font-weight: 700;
  font-size: 1.05rem;
  color: var(--primary-dark);
  text-decoration: none;
}
.mis-pedidos__tienda:hover {
  text-decoration: underline;
}
.mis-pedidos__fecha {
  margin: 0.25rem 0 0;
  font-size: 0.85rem;
  color: var(--text-muted);
}
.mis-pedidos__card-meta {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.35rem;
}
.mis-pedidos__pill {
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.2rem 0.55rem;
  border-radius: 999px;
  background: var(--code-bg);
  color: var(--text-h);
}
.mis-pedidos__pill--ok {
  background: #ecfdf5;
  color: #065f46;
}
.mis-pedidos__pill--err {
  background: #fef2f2;
  color: #991b1b;
}
.mis-pedidos__pill--pending {
  background: #fffbeb;
  color: #92400e;
}
.mis-pedidos__pill--progress {
  background: #eff6ff;
  color: #1d4ed8;
}
.mis-pedidos__total {
  font-weight: 800;
  font-size: 1.1rem;
  color: var(--text-h);
}
.mis-pedidos__seguimiento {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem 0;
  list-style: none;
  margin: 0.85rem 0 0;
  padding: 0;
}
.mis-pedidos__paso {
  flex: 1 1 22%;
  min-width: 4.5rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.35rem;
  position: relative;
  text-align: center;
}
.mis-pedidos__paso:not(:last-child)::after {
  content: '';
  position: absolute;
  top: 0.45rem;
  left: calc(50% + 0.5rem);
  width: calc(100% - 1rem);
  height: 2px;
  background: var(--border, #e5e7eb);
  z-index: 0;
}
.mis-pedidos__paso--hecho:not(:last-child)::after {
  background: #34d399;
}
.mis-pedidos__paso-dot {
  width: 0.9rem;
  height: 0.9rem;
  border-radius: 50%;
  border: 2px solid var(--border-strong, #d1d5db);
  background: var(--surface, #fff);
  z-index: 1;
}
.mis-pedidos__paso--hecho .mis-pedidos__paso-dot {
  border-color: #10b981;
  background: #10b981;
}
.mis-pedidos__paso--actual .mis-pedidos__paso-dot {
  border-color: #2563eb;
  background: #2563eb;
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.25);
}
.mis-pedidos__paso-label {
  font-size: 0.72rem;
  color: var(--text-muted);
  line-height: 1.2;
}
.mis-pedidos__paso--hecho .mis-pedidos__paso-label,
.mis-pedidos__paso--actual .mis-pedidos__paso-label {
  color: var(--text-h);
  font-weight: 600;
}
.mis-pedidos__hint {
  margin: 0.75rem 0 0;
  font-size: 0.85rem;
  color: var(--text-muted);
}
.mis-pedidos__hint--err {
  color: #b91c1c;
}
.mis-pedidos__toggle {
  margin-top: 0.75rem;
  padding: 0;
  border: none;
  background: none;
  color: var(--accent-dark);
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
  text-decoration: underline;
}
.mis-pedidos__lineas {
  list-style: none;
  margin: 0.65rem 0 0;
  padding: 0;
  border-top: 1px solid var(--border);
  padding-top: 0.65rem;
}
.mis-pedidos__lineas li {
  display: grid;
  grid-template-columns: 1fr auto auto;
  gap: 0.5rem;
  font-size: 0.88rem;
  padding: 0.35rem 0;
  border-bottom: 1px solid var(--border);
}
.mis-pedidos__lineas li:last-child {
  border-bottom: none;
}
</style>
