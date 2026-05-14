import type { ComputedRef } from 'vue'
import { ref, watch } from 'vue'
import { apiUrl } from '../config/api'
import type { NegocioPublico, ProductoPublico } from '../types/api'
import { normalizarProductoPublicoDto } from '../utils/normalizarProductoApi'

export function useTiendaCatalogo(slug: ComputedRef<string>) {
  const negocio = ref<NegocioPublico | null>(null)
  const productos = ref<ProductoPublico[]>([])
  const loading = ref(true)
  const error = ref<string | null>(null)

  async function cargar() {
    loading.value = true
    error.value = null
    negocio.value = null
    productos.value = []

    const s = slug.value.trim()
    if (!s) {
      error.value = 'Slug vacío.'
      loading.value = false
      return
    }

    try {
      const rNeg = await fetch(apiUrl(`/api/negocios/${encodeURIComponent(s)}`))
      if (!rNeg.ok) {
        error.value =
          rNeg.status === 404
            ? `No hay tienda con slug «${s}». Creala en la API o probá otro slug.`
            : `Error al cargar la tienda (${rNeg.status}).`
        return
      }
      negocio.value = (await rNeg.json()) as NegocioPublico

      const rProd = await fetch(
        apiUrl(`/api/negocios/${encodeURIComponent(s)}/productos`),
      )
      if (!rProd.ok) {
        error.value = `No se pudieron cargar los productos (${rProd.status}).`
        return
      }
      const rawProd = (await rProd.json()) as unknown
      productos.value = Array.isArray(rawProd)
        ? rawProd.map((x) => normalizarProductoPublicoDto(x as Record<string, unknown>))
        : []
    } catch {
      error.value =
        'No se pudo conectar con la API. ¿Está el backend en el puerto 5037 y el front con npm run dev?'
    } finally {
      loading.value = false
    }
  }

  watch(slug, () => {
    void cargar()
  }, { immediate: true })

  return { negocio, productos, loading, error, cargar }
}
