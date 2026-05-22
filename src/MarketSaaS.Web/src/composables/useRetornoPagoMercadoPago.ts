import type { LocationQuery } from 'vue-router'
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { apiUrl } from '../config/api'

export type RetornoPagoTipo = 'ok' | 'error' | 'pending'

export interface RetornoPagoMensaje {
  tipo: RetornoPagoTipo
  texto: string
}

export interface ConfirmarPagoRetornoDto {
  procesado: boolean
  pedidoId?: string | null
  estadoPedido?: string | null
  mensaje: string
}

function normalizarQuery(val: unknown): string {
  if (val == null) return ''
  const s = Array.isArray(val) ? val[0] : val
  return String(s ?? '').trim()
}

/** Interpreta query de MP (collection_status) o nuestra (?pago=ok|error|pending). */
export function mensajeDesdeQueryPago(query: LocationQuery): RetornoPagoMensaje | null {
  const pago = normalizarQuery(query.pago).toLowerCase()
  const collectionStatus = normalizarQuery(query.collection_status).toLowerCase()
  const status = normalizarQuery(query.status).toLowerCase()

  const aprobado =
    pago === 'ok' ||
    collectionStatus === 'approved' ||
    status === 'approved'
  if (aprobado) {
    return {
      tipo: 'ok',
      texto:
        '¡Pago recibido! Confirmando tu pedido y actualizando el stock…',
    }
  }

  const rechazado =
    pago === 'error' ||
    collectionStatus === 'rejected' ||
    collectionStatus === 'cancelled' ||
    status === 'rejected' ||
    status === 'cancelled'
  if (rechazado) {
    return {
      tipo: 'error',
      texto: 'El pago no se completó. Podés volver a intentar desde el carrito.',
    }
  }

  const pendiente =
    pago === 'pending' ||
    collectionStatus === 'pending' ||
    collectionStatus === 'in_process' ||
    status === 'pending' ||
    status === 'in_process'
  if (pendiente) {
    return {
      tipo: 'pending',
      texto: 'El pago está pendiente. Te avisaremos cuando se acredite.',
    }
  }

  return null
}

function queryTieneIdsMp(query: LocationQuery): boolean {
  return !!(
    normalizarQuery(query.payment_id) ||
    normalizarQuery(query.collection_id) ||
    normalizarQuery(query.merchant_order_id) ||
    normalizarQuery(query.external_reference)
  )
}

async function confirmarPagoEnApi(
  slug: string,
  query: LocationQuery,
): Promise<ConfirmarPagoRetornoDto | null> {
  if (!queryTieneIdsMp(query)) return null

  try {
    const res = await fetch(
      apiUrl(`/api/negocios/${encodeURIComponent(slug)}/pedidos/mercadopago/confirmar-retorno`),
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          paymentId: normalizarQuery(query.payment_id) || undefined,
          collectionId: normalizarQuery(query.collection_id) || undefined,
          externalReference: normalizarQuery(query.external_reference) || undefined,
          merchantOrderId: normalizarQuery(query.merchant_order_id) || undefined,
        }),
      },
    )
    if (!res.ok) return null
    const raw = (await res.json()) as Record<string, unknown>
    return {
      procesado: Boolean(raw.procesado ?? raw.Procesado),
      pedidoId: (raw.pedidoId ?? raw.PedidoId) as string | null | undefined,
      estadoPedido: (raw.estadoPedido ?? raw.EstadoPedido) as string | null | undefined,
      mensaje: String(raw.mensaje ?? raw.Mensaje ?? ''),
    }
  } catch {
    return null
  }
}

export function useRetornoPagoMercadoPago(
  slug: () => string,
  opciones?: { onDespuesConfirmar?: () => void | Promise<void> },
) {
  const route = useRoute()
  const router = useRouter()
  const retornoPago = ref<RetornoPagoMensaje | null>(null)
  const confirmandoPago = ref(false)

  onMounted(() => {
    const query = { ...route.query }
    const msg = mensajeDesdeQueryPago(query)
    if (!msg) return

    retornoPago.value = msg
    const s = slug().trim()
    if (!s) return

    void (async () => {
      if (msg.tipo === 'ok' || msg.tipo === 'pending' || queryTieneIdsMp(query)) {
        confirmandoPago.value = true
        const resultado = await confirmarPagoEnApi(s, query)
        confirmandoPago.value = false

        if (resultado?.procesado && msg.tipo === 'ok') {
          retornoPago.value = {
            tipo: 'ok',
            texto:
              resultado.mensaje ||
              '¡Pago confirmado! El stock de la tienda ya está actualizado.',
          }
          await opciones?.onDespuesConfirmar?.()
        } else if (resultado && !resultado.procesado && msg.tipo === 'ok') {
          retornoPago.value = {
            tipo: 'pending',
            texto:
              resultado.mensaje ||
              'Pago recibido en Mercado Pago; el pedido se confirmará en breve.',
          }
        } else if (resultado?.procesado && msg.tipo === 'error') {
          await opciones?.onDespuesConfirmar?.()
        }
      }

      await router.replace({ name: 'tienda', params: { slug: s }, query: {} })
    })()
  })

  return { retornoPago, confirmandoPago }
}
