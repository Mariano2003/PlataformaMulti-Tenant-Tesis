import type { ComputedRef, Ref } from 'vue'
import { ref, watch } from 'vue'
import { apiUrl } from '../config/api'
import type { CategoriaPublico, NegocioPublico, ProductoPublico } from '../types/api'
import { normalizarProductoPublicoDto } from '../utils/normalizarProductoApi'
import { parsePaginaResponse } from '../utils/parsePaginaResponse'

export function useTiendaCatalogo(
  slug: ComputedRef<string>,
  categoriaId?: Ref<string | null>,
  buscar?: Ref<string>,
  pagina?: Ref<number>,
) {
  const negocio = ref<NegocioPublico | null>(null)
  const categorias = ref<CategoriaPublico[]>([])
  const productos = ref<ProductoPublico[]>([])
  const total = ref(0)
  const totalPaginas = ref(1)
  const tamanoPagina = 12
  const loading = ref(true)
  const error = ref<string | null>(null)

  async function cargarProductos(s: string) {
    const params = new URLSearchParams()
    params.set('pagina', String(pagina?.value ?? 1))
    params.set('tamano', String(tamanoPagina))
    const cat = categoriaId?.value?.trim()
    if (cat) params.set('categoriaId', cat)
    const q = buscar?.value?.trim()
    if (q) params.set('buscar', q)

    const rProd = await fetch(
      apiUrl(`/api/negocios/${encodeURIComponent(s)}/productos?${params.toString()}`),
    )
    if (!rProd.ok) {
      error.value = `No se pudieron cargar los productos (${rProd.status}).`
      return false
    }
    const paginado = parsePaginaResponse(await rProd.json(), normalizarProductoPublicoDto)
    productos.value = paginado.items
    total.value = paginado.total
    totalPaginas.value = paginado.totalPaginas
    if (pagina) pagina.value = paginado.pagina
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

      await cargarCategorias(s)

      const catId = categoriaId?.value?.trim()
      if (catId && !categorias.value.some((c) => c.id === catId)) {
        if (categoriaId) categoriaId.value = null
      }

      await cargarProductos(s)
    } catch {
      error.value =
        'No se pudo conectar con la API. ¿Está el backend en el puerto 5037 y el front con npm run dev?'
    } finally {
      loading.value = false
    }
  }

  watch(
    [slug, () => categoriaId?.value ?? null, () => buscar?.value ?? '', () => pagina?.value ?? 1],
    () => {
      void cargar()
    },
    { immediate: true },
  )

  return { negocio, categorias, productos, total, totalPaginas, tamanoPagina, loading, error, cargar }
}
