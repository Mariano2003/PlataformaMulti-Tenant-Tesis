import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import type { ProductoPublico } from '../types/api'

const STORAGE_KEY = 'marketsaas.carrito.v1'

export interface LineaCarrito {
  productoId: string
  nombre: string
  /** Copiado al agregar; se muestra en checkout. */
  imagenUrl?: string | null
  precioUnitario: number
  cantidad: number
  stockMax: number
}

export const useCarritoStore = defineStore('carrito', () => {
  const slugTienda = ref<string | null>(null)
  const items = ref<LineaCarrito[]>([])

  /** Asocia el carrito a una tienda; si cambia el slug, vacía el carrito. */
  function setTienda(slug: string) {
    const s = slug.trim()
    if (!s) return
    if (slugTienda.value !== s) {
      slugTienda.value = s
      items.value = []
    }
  }

  /** Unidades de este producto ya en el carrito (misma tienda). */
  function cantidadEnCarrito(productoId: string): number {
    return items.value.find((i) => i.productoId === productoId)?.cantidad ?? 0
  }

  /** Stock que aún se puede sumar al carrito (servidor − ya en carrito). */
  function stockDisponible(producto: ProductoPublico): number {
    const enCarrito = cantidadEnCarrito(producto.id)
    return Math.max(0, producto.stock - enCarrito)
  }

  function agregar(producto: ProductoPublico, cantidad = 1) {
    if (!slugTienda.value) return
    if (producto.stock < 1) return
    const disponible = stockDisponible(producto)
    if (disponible < 1) return
    const q = Math.min(Math.max(1, Math.floor(cantidad)), disponible)
    const existente = items.value.find((i) => i.productoId === producto.id)
    if (existente) {
      existente.cantidad = Math.min(existente.cantidad + q, producto.stock)
      existente.stockMax = producto.stock
      existente.imagenUrl = producto.imagenUrl ?? null
      existente.nombre = producto.nombre
      existente.precioUnitario = producto.precio
    } else {
      items.value.push({
        productoId: producto.id,
        nombre: producto.nombre,
        imagenUrl: producto.imagenUrl ?? null,
        precioUnitario: producto.precio,
        cantidad: q,
        stockMax: producto.stock,
      })
    }
  }

  /** Tras recargar catálogo: actualiza límites y saca líneas si ya no hay stock o el producto desapareció. */
  function syncLineasConCatalogo(catalogo: ProductoPublico[]) {
    const map = new Map(catalogo.map((p) => [p.id, p]))
    const nuevas: LineaCarrito[] = []
    for (const linea of items.value) {
      const p = map.get(linea.productoId)
      if (!p || !p.activo) continue
      const q = Math.min(linea.cantidad, p.stock)
      if (q < 1) continue
      nuevas.push({
        ...linea,
        stockMax: p.stock,
        cantidad: q,
        precioUnitario: p.precio,
        nombre: p.nombre,
        imagenUrl: p.imagenUrl ?? linea.imagenUrl,
      })
    }
    items.value = nuevas
  }

  function setCantidad(productoId: string, cantidad: number) {
    const linea = items.value.find((i) => i.productoId === productoId)
    if (!linea) return
    linea.cantidad = Math.min(
      Math.max(1, Math.floor(cantidad)),
      linea.stockMax,
    )
  }

  function remover(productoId: string) {
    items.value = items.value.filter((i) => i.productoId !== productoId)
  }

  function vaciar() {
    items.value = []
  }

  function persistir() {
    if (typeof localStorage === 'undefined') return
    try {
      localStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({
          slugTienda: slugTienda.value,
          items: items.value,
        }),
      )
    } catch {
      /* quota u otro */
    }
  }

  function hidratar() {
    if (typeof localStorage === 'undefined') return
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (!raw) return
      const data = JSON.parse(raw) as {
        slugTienda?: string | null
        items?: LineaCarrito[]
      }
      if (typeof data.slugTienda === 'string' && data.slugTienda)
        slugTienda.value = data.slugTienda
      if (Array.isArray(data.items)) items.value = data.items
    } catch {
      /* JSON inválido */
    }
  }

  hidratar()

  watch(
    () => [slugTienda.value, items.value] as const,
    () => persistir(),
    { deep: true },
  )

  const totalUnidades = computed(() =>
    items.value.reduce((acc, i) => acc + i.cantidad, 0),
  )

  const subtotal = computed(() =>
    items.value.reduce((acc, i) => acc + i.precioUnitario * i.cantidad, 0),
  )

  return {
    slugTienda,
    items,
    setTienda,
    cantidadEnCarrito,
    stockDisponible,
    agregar,
    syncLineasConCatalogo,
    setCantidad,
    remover,
    vaciar,
    totalUnidades,
    subtotal,
  }
})
