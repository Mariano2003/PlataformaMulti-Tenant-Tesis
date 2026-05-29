<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AdminNav from '../../components/admin/AdminNav.vue'
import { useAuthedFetch } from '../../composables/useAuthedFetch'
import { useAuthStore } from '../../stores/auth'
import type { PedidoListDto } from '../../types/api'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const authedFetch = useAuthedFetch()

const slug = computed(() => (route.params.slug as string) || '')

const pedidos = ref<PedidoListDto[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)

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

async function cargarPedidos() {
  cargando.value = true
  error.value = null
  pedidos.value = []
  const s = slug.value
  if (!s) {
    error.value = 'Slug inválido.'
    cargando.value = false
    return
  }
  try {
    const res = await authedFetch(
      `/api/negocios/${encodeURIComponent(s)}/admin/pedidos?limite=100`,
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
    pedidos.value = (await res.json()) as PedidoListDto[]
  } catch {
    error.value = 'Error de red al cargar pedidos.'
  } finally {
    cargando.value = false
  }
}

function salir() {
  auth.cerrarSesion()
}

onMounted(() => {
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
        <button type="button" class="btn-ghost" @click="cargarPedidos">Actualizar</button>
        <button type="button" class="btn-ghost" @click="salir">Salir</button>
      </div>
    </header>

    <AdminNav :slug="slug" />

    <nav class="admin-breadcrumb">
      <RouterLink to="/">Inicio</RouterLink>
      <span class="sep">·</span>
      <RouterLink :to="{ name: 'tienda', params: { slug } }">Ver tienda pública</RouterLink>
    </nav>

    <p v-if="cargando" class="admin-msg">Cargando…</p>
    <p v-else-if="error" class="admin-msg admin-msg--error">{{ error }}</p>

    <div v-else-if="!pedidos.length" class="admin-msg">No hay pedidos en esta tienda.</div>

    <div v-else class="admin-table-wrap">
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
            <td><span class="admin-pill">{{ p.estado }}</span></td>
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
  </div>
</template>
