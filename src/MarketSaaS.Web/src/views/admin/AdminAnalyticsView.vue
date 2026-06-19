<script setup lang="ts">
import {
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  Title,
  Tooltip,
} from 'chart.js'
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { Bar } from 'vue-chartjs'
import AdminNav from '../../components/admin/AdminNav.vue'
import { useAuthedFetch } from '../../composables/useAuthedFetch'
import { useAuthStore } from '../../stores/auth'
import type { VentasResumenDto } from '../../types/api'

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale)

function chartPalette() {
  const cs = getComputedStyle(document.documentElement)
  const pick = (name: string, fallback: string) => {
    const v = cs.getPropertyValue(name).trim()
    return v || fallback
  }
  return {
    text: pick('--text-h', '#111827'),
    muted: pick('--text', '#6b7280'),
    border: pick('--border', '#e5e7eb'),
    accent: pick('--accent', '#aa3bff'),
    accent2: pick('--primary', '#6366f1'),
  }
}

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const authedFetch = useAuthedFetch()

const slug = computed(() => (route.params.slug as string) || '')

const resumen = ref<VentasResumenDto | null>(null)
const cargando = ref(true)
const error = ref<string | null>(null)

const precioFmt = new Intl.NumberFormat('es-AR', {
  style: 'currency',
  currency: 'ARS',
  maximumFractionDigits: 0,
})

const ventasPorDiaData = computed(() => {
  const t = chartPalette()
  const r = resumen.value
  if (!r?.ventasPorDia?.length) {
    return {
      labels: [] as string[],
      datasets: [
        { label: 'Monto (ARS)', data: [] as number[], backgroundColor: t.accent, yAxisID: 'y' },
        { label: 'Pedidos', data: [] as number[], backgroundColor: t.accent2, yAxisID: 'y1' },
      ],
    }
  }
  return {
    labels: r.ventasPorDia.map((v) => v.fecha.slice(5)),
    datasets: [
      {
        label: 'Monto pagado (ARS)',
        data: r.ventasPorDia.map((v) => v.montoTotal),
        backgroundColor: t.accent,
        borderRadius: 6,
        yAxisID: 'y',
      },
      {
        label: 'Cantidad de pedidos',
        data: r.ventasPorDia.map((v) => v.cantidadPedidos),
        backgroundColor: `${t.accent2}99`,
        borderRadius: 6,
        yAxisID: 'y1',
      },
    ],
  }
})

const ventasPorDiaOptions = computed(() => {
  const t = chartPalette()
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true,
        labels: { color: t.text, boxWidth: 12, padding: 12 },
      },
      title: {
        display: true,
        text: 'Ventas y pedidos por día (últimos 30 días, UTC)',
        color: t.muted,
        font: { size: 13, weight: 'bold' as const },
      },
    },
    scales: {
      x: {
        ticks: { color: t.muted, maxRotation: 40 },
        grid: { color: t.border },
      },
      y: {
        type: 'linear' as const,
        position: 'left' as const,
        beginAtZero: true,
        ticks: { color: t.muted },
        grid: { color: t.border },
        title: { display: true, text: 'ARS', color: t.muted },
      },
      y1: {
        type: 'linear' as const,
        position: 'right' as const,
        beginAtZero: true,
        ticks: { color: t.muted, stepSize: 1 },
        grid: { drawOnChartArea: false },
        title: { display: true, text: 'Pedidos', color: t.muted },
      },
    },
  }
})

const topProductosData = computed(() => {
  const t = chartPalette()
  const items = resumen.value?.productosTop ?? []
  if (!items.length) {
    return {
      labels: [] as string[],
      datasets: [{ label: 'Ingresos (ARS)', data: [] as number[], backgroundColor: t.accent }],
    }
  }
  const ordenados = [...items].reverse()
  return {
    labels: ordenados.map((p) => p.nombre),
    datasets: [
      {
        label: 'Ingresos (ARS)',
        data: ordenados.map((p) => p.montoTotal),
        backgroundColor: t.accent,
        borderRadius: 6,
      },
    ],
  }
})

const topProductosOptions = computed(() => {
  const t = chartPalette()
  return {
    indexAxis: 'y' as const,
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      title: {
        display: true,
        text: 'Top productos por ingresos (30 días)',
        color: t.muted,
        font: { size: 13, weight: 'bold' as const },
      },
      tooltip: {
        callbacks: {
          afterLabel(ctx: { raw: unknown; dataIndex: number }) {
            const items = resumen.value?.productosTop ?? []
            const idx = items.length - 1 - ctx.dataIndex
            const p = items[idx]
            return p ? `${p.cantidadVendida} unidades vendidas` : ''
          },
        },
      },
    },
    scales: {
      x: {
        beginAtZero: true,
        ticks: { color: t.muted },
        grid: { color: t.border },
      },
      y: {
        ticks: { color: t.muted },
        grid: { color: t.border },
      },
    },
  }
})

async function cargar() {
  cargando.value = true
  error.value = null
  resumen.value = null
  const s = slug.value
  if (!s) {
    error.value = 'Slug inválido.'
    cargando.value = false
    return
  }
  try {
    const res = await authedFetch(
      `/api/negocios/${encodeURIComponent(s)}/admin/analytics/resumen`,
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
      error.value = 'No tenés permiso para ver analytics de esta tienda.'
      return
    }
    if (!res.ok) {
      error.value = `Error ${res.status}`
      return
    }
    resumen.value = (await res.json()) as VentasResumenDto
  } catch {
    error.value = 'Error de red al cargar resumen.'
  } finally {
    cargando.value = false
  }
}

function salir() {
  auth.cerrarSesion()
}

onMounted(() => {
  void cargar()
})
</script>

<template>
  <div class="admin-page admin-page--wide">
    <header class="admin-head">
      <div>
        <h1>Analytics</h1>
        <p class="admin-sub">
          Tienda <code>{{ slug }}</code>
          <span v-if="auth.usuario"> · {{ auth.usuario.email }} ({{ auth.usuario.rol }})</span>
        </p>
      </div>
      <div class="admin-actions">
        <button type="button" class="btn-ghost" @click="cargar">Actualizar</button>
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

    <template v-else-if="resumen">
      <section class="admin-kpis">
        <div class="admin-kpi">
          <span class="admin-kpi-label">Ingresos (30 días, pagados)</span>
          <strong class="admin-kpi-val">{{ precioFmt.format(resumen.montoTotalVentana) }}</strong>
        </div>
        <div class="admin-kpi">
          <span class="admin-kpi-label">Pedidos pagados (30 días)</span>
          <strong class="admin-kpi-val">{{ resumen.pedidosPagadosVentana }}</strong>
        </div>
        <div class="admin-kpi">
          <span class="admin-kpi-label">Ticket promedio (30 días)</span>
          <strong class="admin-kpi-val">{{ precioFmt.format(resumen.ticketPromedioVentana) }}</strong>
        </div>
      </section>

      <div class="admin-charts">
        <div class="admin-chart-box">
          <Bar :data="ventasPorDiaData" :options="ventasPorDiaOptions" />
        </div>
        <div
          class="admin-chart-box admin-chart-box--compact"
          :class="{ 'admin-chart-box--empty': !resumen.productosTop?.length }"
        >
          <Bar
            v-if="resumen.productosTop?.length"
            :data="topProductosData"
            :options="topProductosOptions"
          />
          <p v-else class="admin-chart-empty">Todavía no hay ventas de productos en los últimos 30 días.</p>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.admin-chart-empty {
  margin: 0;
  padding: 2rem 1rem;
  text-align: center;
  color: var(--text-muted);
  font-size: 0.95rem;
}
.admin-chart-box--empty {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 16rem;
}
</style>
