import type { PedidoClienteListItemDto, PedidoLineaListDto } from '../types/api'

function num(o: Record<string, unknown>, ...keys: string[]) {
  for (const k of keys) {
    const v = o[k]
    if (typeof v === 'number' && !Number.isNaN(v)) return v
  }
  return 0
}

function str(o: Record<string, unknown>, ...keys: string[]) {
  for (const k of keys) {
    const v = o[k]
    if (typeof v === 'string' && v.trim()) return v.trim()
  }
  return ''
}

export function normalizarPedidoLinea(raw: Record<string, unknown>): PedidoLineaListDto {
  return {
    productoId: str(raw, 'productoId', 'ProductoId') || '—',
    nombre: str(raw, 'nombre', 'Nombre') || 'Producto',
    cantidad: num(raw, 'cantidad', 'Cantidad'),
    precioUnitario: num(raw, 'precioUnitario', 'PrecioUnitario'),
    subtotal: num(raw, 'subtotal', 'Subtotal'),
  }
}

export function normalizarPedidoCliente(raw: Record<string, unknown>): PedidoClienteListItemDto {
  const lineasRaw = raw.lineas ?? raw.Lineas
  const lineas = Array.isArray(lineasRaw)
    ? lineasRaw.map((x) => normalizarPedidoLinea(x as Record<string, unknown>))
    : []

  return {
    id: str(raw, 'id', 'Id'),
    negocioId: str(raw, 'negocioId', 'NegocioId'),
    negocioSlug: str(raw, 'negocioSlug', 'NegocioSlug'),
    negocioNombre: str(raw, 'negocioNombre', 'NegocioNombre') || 'Tienda',
    estado: str(raw, 'estado', 'Estado'),
    total: num(raw, 'total', 'Total'),
    creadoEn: str(raw, 'creadoEn', 'CreadoEn'),
    lineas,
  }
}

export function resumenLineasPedido(lineas: PedidoLineaListDto[]) {
  if (!lineas.length) return ''
  return lineas.map((l) => `${l.cantidad}× ${l.nombre}`).join(' · ')
}
