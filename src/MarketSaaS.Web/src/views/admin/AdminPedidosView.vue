<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AdminNav from '../../components/admin/AdminNav.vue'
import AdminPaginacion from '../../components/admin/AdminPaginacion.vue'
import { useAdminPedidosNovedades } from '../../composables/useAdminPedidosNovedades'
import { useAuthedFetch } from '../../composables/useAuthedFetch'
import { useAuthStore } from '../../stores/auth'
import type { PedidoListDto } from '../../types/api'
import {
  ESTADOS_PEDIDO_ADMIN,
  etiquetaEstadoPedido,
  pedidoAdminPuedeGestionar,
} from '../../types/api'
import { parsePaginaResponse } from '../../utils/parsePaginaResponse'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const authedFetch = useAuthedFetch()

const slug = computed(() => (route.params.slug as string) || '')
const { marcarVisitado } = useAdminPedidosNovedades(slug)

const pedidos = ref<PedidoListDto[]>([])
const pagina = ref(1)
const totalPaginas = ref(1)
const total = ref(0)
const tamanoPagina = 20
const cargando = ref(true)
const error = ref<string | null>(null)
const msg = ref<string | null>(null)
const actualizandoId = ref<string | null>(null)

const precioFmt = new Intl.NumberFormat('es-AR', {
  style: 'currency',
  currency: 'ARS',
  maximumFractionDigits: 2,
})

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

async function cargarPedidos(paginaDestino = pagina.value) {
  cargando.value = true
  error.value = null
  msg.value = null
  pedidos.value = []
  const s = slug.value
  if (!s) {
    error.value = 'Slug inválido.'
    cargando.value = false
    return
  }
  try {
    const res = await authedFetch(
      `/api/negocios/${encodeURIComponent(s)}/admin/pedidos?pagina=${paginaDestino}&tamano=${tamanoPagina}`,
    )
    if (res.status === 401) {
      auth.cerrarSesion()
      await router.replace({
        name: 'admin-login',
        params: { slug: s },
        query: { redirect: route.fullPath },
      })
      return
    }
    if (res.status === 403) {
      error.value =
        'No tenés permiso para este negocio (el token debe ser AdminTienda de esta tienda o SuperAdmin).'
      return
    }
    if (!res.ok) {
      error.value = `Error ${res.status}`
      return
    }
    const paginado = parsePaginaResponse<PedidoListDto>(await res.json())
    pedidos.value = paginado.items
    pagina.value = paginado.pagina
    totalPaginas.value = paginado.totalPaginas
    total.value = paginado.total
  } catch {
    error.value = 'Error de red al cargar pedidos.'
  } finally {
    cargando.value = false
  }
}

async function cambiarEstado(p: PedidoListDto, nuevoEstado: string) {
  if (nuevoEstado === p.estado) return
  msg.value = null
  actualizandoId.value = p.id
  const s = slug.value
  try {
    const res = await authedFetch(
      `/api/negocios/${encodeURIComponent(s)}/admin/pedidos/${encodeURIComponent(p.id)}/estado`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ estado: nuevoEstado }),
      },
    )
    if (!res.ok) {
      try {
        const j = (await res.json()) as { error?: string }
        msg.value = j.error ?? `Error ${res.status}`
      } catch {
        msg.value = `Error ${res.status}`
      }
      return
    }
    const actualizado = (await res.json()) as PedidoListDto
    const idx = pedidos.value.findIndex((x) => x.id === p.id)
    if (idx >= 0) pedidos.value[idx] = actualizado
    msg.value = 'Estado actualizado.'
  } catch {
    msg.value = 'Error de red al actualizar el estado.'
  } finally {
    actualizandoId.value = null
  }
}

function paginaAnterior() {
  if (pagina.value <= 1) return
  void cargarPedidos(pagina.value - 1)
}

function paginaSiguiente() {
  if (pagina.value >= totalPaginas.value) return
  void cargarPedidos(pagina.value + 1)
}

function salir() {
  auth.cerrarSesion()
}

onMounted(() => {
  marcarVisitado()
  void cargarPedidos()
})
</script>

<template>
  <div class="admin-page">
    <header class="admin-head">
      <div>
        <h1>Pedidos</h1>
        <p class="admin-sub">
          Tienda <code>{{ slug }}</code>
          <span v-if="auth.usuario"> · {{ auth.usuario.email }} ({{ auth.usuario.rol }})</span>
        </p>
      </div>
      <div class="admin-actions">
        <button type="button" class="btn-ghost" @click="() => void cargarPedidos()">Actualizar</button>
        <button type="button" class="btn-ghost" @click="salir">Salir</button>
      </div>
    </header>

    <AdminNav :slug="slug" />

    <nav class="admin-breadcrumb">
      <RouterLink to="/">Inicio</RouterLink>
      <span class="sep">·</span>
      <RouterLink :to="{ name: 'tienda', params: { slug } }">Ver tienda pública</RouterLink>
    </nav>

    <p v-if="msg" class="admin-msg">{{ msg }}</p>
    <p v-if="cargando" class="admin-msg">Cargando…</p>
    <p v-else-if="error" class="admin-msg admin-msg--error">{{ error }}</p>

    <div v-else-if="!pedidos.length" class="admin-msg">No hay pedidos en esta tienda.</div>

    <template v-else>
      <div class="admin-table-wrap">
        <table class="admin-table">
          <thead>
            <tr>
              <th>Fecha</th>
              <th>Estado</th>
              <th>Cliente</th>
              <th>Total</th>
              <th>MP</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in pedidos" :key="p.id">
              <td>{{ formatearFecha(p.creadoEn) }}</td>
              <td>
                <select
                  v-if="pedidoAdminPuedeGestionar(p.estado)"
                  class="estado-select"
                  :value="p.estado"
                  :disabled="actualizandoId === p.id"
                  @change="cambiarEstado(p, ($event.target as HTMLSelectElement).value)"
                >
                  <option :value="p.estado">{{ etiquetaEstadoPedido(p.estado) }}</option>
                  <option
                    v-for="e in ESTADOS_PEDIDO_ADMIN"
                    :key="e.valor"
                    :value="e.valor"
                    :disabled="e.valor === p.estado"
                  >
                    {{ e.etiqueta }}
                  </option>
                </select>
                <span v-else class="admin-pill">{{ etiquetaEstadoPedido(p.estado) }}</span>
              </td>
              <td>{{ p.clienteEmail ?? '—' }}</td>
              <td>{{ precioFmt.format(p.total) }}</td>
              <td class="admin-mono">
                <template v-if="p.mercadoPagoPaymentId || p.mercadoPagoPreferenceId">
                  {{ p.mercadoPagoPaymentId || p.mercadoPagoPreferenceId }}
                </template>
                <template v-else>—</template>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <AdminPaginacion
        :pagina="pagina"
        :total-paginas="totalPaginas"
        :total="total"
        :cargando="cargando"
        @anterior="paginaAnterior"
        @siguiente="paginaSiguiente"
      />
    </template>
  </div>
</template>

<style scoped>
.estado-select {
  max-width: 11rem;
  padding: 0.3rem 0.45rem;
  border-radius: 6px;
  border: 1px solid var(--border, #d1d5db);
  font-size: 0.85rem;
  background: var(--surface, #fff);
}
</style>
