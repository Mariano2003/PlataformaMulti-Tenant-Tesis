import { describe, expect, it } from 'vitest'
import {
  claseEstadoPedidoCliente,
  etiquetaEstadoPedido,
  indiceSeguimientoPedido,
  pedidoAdminPuedeGestionar,
  pedidoDebeAutoActualizar,
  pedidoMuestraSeguimiento,
} from '../types/api'

describe('etiquetaEstadoPedido', () => {
  it('traduce estados conocidos', () => {
    expect(etiquetaEstadoPedido('Pagado')).toBe('Pagado')
    expect(etiquetaEstadoPedido('EnPreparacion')).toBe('En preparación')
    expect(etiquetaEstadoPedido('PendientePago')).toBe('Pendiente de pago')
  })

  it('devuelve el estado crudo si no está mapeado', () => {
    expect(etiquetaEstadoPedido('EstadoRaro')).toBe('EstadoRaro')
  })
})

describe('seguimiento de pedido', () => {
  it('indiceSeguimientoPedido para flujo post-pago', () => {
    expect(indiceSeguimientoPedido('Pagado')).toBe(0)
    expect(indiceSeguimientoPedido('EnPreparacion')).toBe(1)
    expect(indiceSeguimientoPedido('Enviado')).toBe(2)
    expect(indiceSeguimientoPedido('Entregado')).toBe(3)
    expect(indiceSeguimientoPedido('Confirmado')).toBe(0)
    expect(indiceSeguimientoPedido('PendientePago')).toBe(-1)
  })

  it('pedidoMuestraSeguimiento solo post-pago', () => {
    expect(pedidoMuestraSeguimiento('Enviado')).toBe(true)
    expect(pedidoMuestraSeguimiento('PendientePago')).toBe(false)
  })

  it('pedidoDebeAutoActualizar hasta estado terminal', () => {
    expect(pedidoDebeAutoActualizar('Pagado')).toBe(true)
    expect(pedidoDebeAutoActualizar('Entregado')).toBe(false)
    expect(pedidoDebeAutoActualizar('Cancelado')).toBe(false)
    expect(pedidoDebeAutoActualizar('Rechazado')).toBe(false)
  })
})

describe('pedidoAdminPuedeGestionar', () => {
  it('permite gestionar estados cobrados o en curso', () => {
    expect(pedidoAdminPuedeGestionar('Pagado')).toBe(true)
    expect(pedidoAdminPuedeGestionar('Enviado')).toBe(true)
    expect(pedidoAdminPuedeGestionar('PendientePago')).toBe(false)
    expect(pedidoAdminPuedeGestionar('Entregado')).toBe(false)
  })
})

describe('claseEstadoPedidoCliente', () => {
  it('asigna clase visual segun estado', () => {
    expect(claseEstadoPedidoCliente('Entregado')).toBe('ok')
    expect(claseEstadoPedidoCliente('Rechazado')).toBe('err')
    expect(claseEstadoPedidoCliente('PendientePago')).toBe('pending')
  })
})
