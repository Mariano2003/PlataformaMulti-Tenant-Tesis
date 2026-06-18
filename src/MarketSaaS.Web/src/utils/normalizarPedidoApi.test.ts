import { describe, expect, it } from 'vitest'
import {
  normalizarPedidoCliente,
  normalizarPedidoLinea,
  resumenLineasPedido,
} from './normalizarPedidoApi'

describe('normalizarPedidoLinea', () => {
  it('lee campos camelCase', () => {
    const linea = normalizarPedidoLinea({
      productoId: 'abc',
      nombre: 'Remera',
      cantidad: 2,
      precioUnitario: 100,
      subtotal: 200,
    })

    expect(linea).toEqual({
      productoId: 'abc',
      nombre: 'Remera',
      cantidad: 2,
      precioUnitario: 100,
      subtotal: 200,
    })
  })

  it('lee campos PascalCase legacy', () => {
    const linea = normalizarPedidoLinea({
      ProductoId: 'xyz',
      Nombre: 'Pantalón',
      Cantidad: 1,
      PrecioUnitario: 50,
      Subtotal: 50,
    })

    expect(linea.nombre).toBe('Pantalón')
    expect(linea.productoId).toBe('xyz')
  })
})

describe('normalizarPedidoCliente', () => {
  it('normaliza pedido con lineas en camelCase', () => {
    const pedido = normalizarPedidoCliente({
      id: 'p1',
      negocioSlug: 'tienda-demo',
      negocioNombre: 'Demo',
      estado: 'Pagado',
      total: 200,
      creadoEn: '2026-06-11T12:00:00Z',
      lineas: [{ productoId: 'a', nombre: 'Remera', cantidad: 2, precioUnitario: 100, subtotal: 200 }],
    })

    expect(pedido.lineas).toHaveLength(1)
    expect(pedido.negocioSlug).toBe('tienda-demo')
    expect(pedido.estado).toBe('Pagado')
  })

  it('normaliza pedido con Lineas PascalCase', () => {
    const pedido = normalizarPedidoCliente({
      Id: 'p2',
      NegocioNombre: 'Otra',
      Estado: 'Enviado',
      Total: 50,
      Lineas: [{ ProductoId: 'x', Nombre: 'Gorra', Cantidad: 1, PrecioUnitario: 50, Subtotal: 50 }],
    })

    expect(pedido.id).toBe('p2')
    expect(pedido.estado).toBe('Enviado')
    expect(pedido.lineas[0]?.nombre).toBe('Gorra')
  })
})

describe('resumenLineasPedido', () => {
  it('arma texto legible', () => {
    const txt = resumenLineasPedido([
      { productoId: 'a', nombre: 'Remera', cantidad: 2, precioUnitario: 100, subtotal: 200 },
      { productoId: 'b', nombre: 'Gorra', cantidad: 1, precioUnitario: 50, subtotal: 50 },
    ])

    expect(txt).toBe('2× Remera · 1× Gorra')
  })

  it('devuelve vacío sin lineas', () => {
    expect(resumenLineasPedido([])).toBe('')
  })
})
