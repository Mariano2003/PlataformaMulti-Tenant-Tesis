import { computed, onMounted, onUnmounted, ref, watch, type ComputedRef } from 'vue'
import { useAuthedFetch } from './useAuthedFetch'

const STORAGE_PREFIX = 'admin-pedidos-visto-'

export function useAdminPedidosNovedades(slug: ComputedRef<string>) {
  const authedFetch = useAuthedFetch()
  const novedades = ref(0)
  const cargando = ref(false)

  function claveStorage() {
    return `${STORAGE_PREFIX}${slug.value.trim()}`
  }

  function leerUltimaVisita(): string {
    try {
      return localStorage.getItem(claveStorage()) ?? new Date(0).toISOString()
    } catch {
      return new Date(0).toISOString()
    }
  }

  function marcarVisitado() {
    try {
      localStorage.setItem(claveStorage(), new Date().toISOString())
    } catch {
      /* ignore */
    }
    novedades.value = 0
  }

  async function refrescar() {
    const s = slug.value.trim()
    if (!s) {
      novedades.value = 0
      return
    }
    cargando.value = true
    try {
      const desde = encodeURIComponent(leerUltimaVisita())
      const res = await authedFetch(
        `/api/negocios/${encodeURIComponent(s)}/admin/pedidos/novedades?desde=${desde}`,
      )
      if (!res.ok) {
        novedades.value = 0
        return
      }
      const data = (await res.json()) as { pedidosPagadosNuevos?: number }
      novedades.value = Math.max(0, Number(data.pedidosPagadosNuevos) || 0)
    } catch {
      novedades.value = 0
    } finally {
      cargando.value = false
    }
  }

  let intervalo: ReturnType<typeof setInterval> | null = null

  watch(slug, () => {
    void refrescar()
  })

  onMounted(() => {
    void refrescar()
    intervalo = setInterval(() => {
      void refrescar()
    }, 60_000)
  })

  onUnmounted(() => {
    if (intervalo) clearInterval(intervalo)
  })

  const hayNovedades = computed(() => novedades.value > 0)

  return { novedades, hayNovedades, cargando, refrescar, marcarVisitado }
}
