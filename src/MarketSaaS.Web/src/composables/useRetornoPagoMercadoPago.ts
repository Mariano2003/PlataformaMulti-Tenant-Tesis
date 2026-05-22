import type { LocationQuery } from 'vue-router'
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

export type RetornoPagoTipo = 'ok' | 'error' | 'pending'

export interface RetornoPagoMensaje {
  tipo: RetornoPagoTipo
  texto: string
}

function normalizarQuery(val: unknown): string {
  if (val == null) return ''
  const s = Array.isArray(val) ? val[0] : val
  return String(s ?? '').trim().toLowerCase()
}

/** Interpreta query de MP (collection_status) o nuestra (?pago=ok|error|pending). */
export function mensajeDesdeQueryPago(query: LocationQuery): RetornoPagoMensaje | null {
  const pago = normalizarQuery(query.pago)
  const collectionStatus = normalizarQuery(query.collection_status)
  const status = normalizarQuery(query.status)

  const aprobado =
    pago === 'ok' ||
    collectionStatus === 'approved' ||
    status === 'approved'
  if (aprobado) {
    return {
      tipo: 'ok',
      texto:
        '¡Pago recibido! Tu pedido se está confirmando. Si no ves el stock actualizado al instante, esperá unos segundos.',
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

/** Lee el retorno de MP una vez, muestra mensaje y limpia la URL (evita recargas raras). */
export function useRetornoPagoMercadoPago(slug: () => string) {
  const route = useRoute()
  const router = useRouter()
  const retornoPago = ref<RetornoPagoMensaje | null>(null)

  onMounted(() => {
    const msg = mensajeDesdeQueryPago(route.query)
    if (!msg) return

    retornoPago.value = msg
    const s = slug().trim()
    if (!s) return

    void router.replace({ name: 'tienda', params: { slug: s }, query: {} })
  })

  return { retornoPago }
}
