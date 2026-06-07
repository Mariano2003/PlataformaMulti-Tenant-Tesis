export interface PaginaResponseDto<T> {
  items: T[]
  pagina: number
  tamano: number
  total: number
  totalPaginas: number
}

export function parsePaginaResponse<T>(
  raw: unknown,
  mapItem?: (x: Record<string, unknown>) => T,
): PaginaResponseDto<T> {
  if (raw && typeof raw === 'object' && !Array.isArray(raw) && 'items' in raw) {
    const o = raw as Record<string, unknown>
    const itemsRaw = o.items
    const items = Array.isArray(itemsRaw)
      ? mapItem
        ? itemsRaw.map((x) => mapItem(x as Record<string, unknown>))
        : (itemsRaw as T[])
      : []
    const pagina = Number(o.pagina) || 1
    const tamano = Number(o.tamano) || items.length || 20
    const total = Number(o.total ?? items.length)
    const totalPaginas = Number(o.totalPaginas) || (tamano > 0 ? Math.ceil(total / tamano) : 1)
    return { items, pagina, tamano, total, totalPaginas }
  }

  const items = Array.isArray(raw)
    ? mapItem
      ? raw.map((x) => mapItem(x as Record<string, unknown>))
      : (raw as T[])
    : []
  return {
    items,
    pagina: 1,
    tamano: items.length || 20,
    total: items.length,
    totalPaginas: 1,
  }
}
