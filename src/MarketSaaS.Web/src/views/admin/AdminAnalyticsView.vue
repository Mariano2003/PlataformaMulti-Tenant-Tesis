<script setup lang="ts">
import {
  ArcElement,
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
import { Bar, Doughnut } from 'vue-chartjs'
import AdminNav from '../../components/admin/AdminNav.vue'
import { useAuthedFetch } from '../../composables/useAuthedFetch'
import { useAuthStore } from '../../stores/auth'
import type { VentasResumenDto } from '../../types/api'

ChartJS.register(
  Title,
  Tooltip,
  Legend,
  BarElement,
  CategoryScale,
  LinearScale,
  ArcElement,
)

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

const barData = computed(() => {
  const t = chartPalette()
  const r = resumen.value
  if (!r?.ventasPorDia?.length) {
    return {
      labels: [] as string[],
      datasets: [{ label: 'Monto (ARS)', data: [] as number[], backgroundColor: t.accent }],
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
      },
    ],
  }
})

const barOptions = computed(() => {
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
        text: 'Ventas por día (últimos 30 días, UTC)',
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
        beginAtZero: true,
        ticks: { color: t.muted },
        grid: { color: t.border },
      },
    },
  }
})

const doughnutData = computed(() => {
  const t = chartPalette()
  const r = resumen.value
  if (!r?.pedidosPorEstado?.length) {
    return {
      labels: [] as string[],
      datasets: [{ data: [] as number[], backgroundColor: [] as string[] }],
    }
  }
  const colors = [
    t.accent,
    '#818cf8',
    '#22d3ee',
    '#f472b6',
    '#a3e635',
    '#fb923c',
    '#94a3b8',
  ]
  return {
    labels: r.pedidosPorEstado.map((p) => p.estado),
    datasets: [
      {
        data: r.pedidosPorEstado.map((p) => p.cantidad),
        backgroundColor: r.pedidosPorEstado.map((_, i) => colors[i % colors.length]!),
        borderWidth: 2,
        borderColor: t.border,
      },
    ],
  }
})

const doughnutOptions = computed(() => {
  const t = chartPalette()
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom' as const,
        labels: { color: t.text, padding: 14 },
      },
      title: {
        display: true,
        text: 'Pedidos por estado',
        color: t.muted,
        font: { size: 13, weight: 'bold' as const },
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

      <section v-if="resumen.productosTop?.length" class="admin-card admin-top-productos">
        <h2>Top productos (30 días)</h2>
        <div class="admin-table-wrap">
          <table class="admin-table">
            <thead>
              <tr>
                <th>Producto</th>
                <th>Unidades</th>
                <th>Ingresos</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="p in resumen.productosTop" :key="p.productoId">
                <td>{{ p.nombre }}</td>
                <td>{{ p.cantidadVendida }}</td>
                <td>{{ precioFmt.format(p.montoTotal) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <div class="admin-charts">
        <div class="admin-chart-box">
          <Bar :data="barData" :options="barOptions" />
        </div>
        <div class="admin-chart-box admin-chart-box--compact">
          <Doughnut :data="doughnutData" :options="doughnutOptions" />
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.admin-top-productos {
  margin-top: 1.25rem;
}
.admin-top-productos h2 {
  margin: 0 0 0.75rem;
  font-size: 1.05rem;
}
</style>
