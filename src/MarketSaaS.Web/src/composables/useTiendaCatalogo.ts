import type { ComputedRef, Ref } from 'vue'
import { ref, watch } from 'vue'
import { apiUrl } from '../config/api'
import type { CategoriaPublico, NegocioPublico, ProductoPublico } from '../types/api'
import { normalizarProductoPublicoDto } from '../utils/normalizarProductoApi'

export function useTiendaCatalogo(
  slug: ComputedRef<string>,
  categoriaId?: Ref<string | null>,
) {
  const negocio = ref<NegocioPublico | null>(null)
  const categorias = ref<CategoriaPublico[]>([])
  const productos = ref<ProductoPublico[]>([])
  const loading = ref(true)
  const error = ref<string | null>(null)

  async function cargarProductos(s: string, catId: string | null) {
    const qs = catId ? `?categoriaId=${encodeURIComponent(catId)}` : ''
    const rProd = await fetch(
      apiUrl(`/api/negocios/${encodeURIComponent(s)}/productos${qs}`),
    )
    if (!rProd.ok) {
      error.value = `No se pudieron cargar los productos (${rProd.status}).`
      return false
    }
    const rawProd = (await rProd.json()) as unknown
    productos.value = Array.isArray(rawProd)
      ? rawProd.map((x) => normalizarProductoPublicoDto(x as Record<string, unknown>))
      : []
    return true
  }

  async function cargarCategorias(s: string) {
    const rCat = await fetch(
      apiUrl(`/api/negocios/${encodeURIComponent(s)}/categorias`),
    )
    if (!rCat.ok) {
      categorias.value = []
      return
    }
    const raw = (await rCat.json()) as unknown
    categorias.value = Array.isArray(raw) ? (raw as CategoriaPublico[]) : []
  }

  async function cargar() {
    loading.value = true
    error.value = null
    negocio.value = null
    productos.value = []
    categorias.value = []

    const s = slug.value.trim()
    if (!s) {
      error.value = 'Slug vacío.'
      loading.value = false
      return
    }

    const catId = categoriaId?.value?.trim() || null

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

      await cargarCategorias(s)

      if (catId && !categorias.value.some((c) => c.id === catId)) {
        if (categoriaId) categoriaId.value = null
      }

      const filtro = categoriaId?.value?.trim() || null
      await cargarProductos(s, filtro)
    } catch {
      error.value =
        'No se pudo conectar con la API. ¿Está el backend en el puerto 5037 y el front con npm run dev?'
    } finally {
      loading.value = false
    }
  }

  watch(
    [slug, () => categoriaId?.value ?? null],
    () => {
      void cargar()
    },
    { immediate: true },
  )

  return { negocio, categorias, productos, loading, error, cargar }
}
