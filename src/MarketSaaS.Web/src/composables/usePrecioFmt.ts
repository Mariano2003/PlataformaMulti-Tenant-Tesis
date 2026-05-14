import { shallowRef } from 'vue'

const fmt = shallowRef(
  new Intl.NumberFormat('es-AR', {
    style: 'currency',
    currency: 'ARS',
    maximumFractionDigits: 2,
  }),
)

export function usePrecioFmt() {
  return (valor: number) => fmt.value.format(valor)
}
