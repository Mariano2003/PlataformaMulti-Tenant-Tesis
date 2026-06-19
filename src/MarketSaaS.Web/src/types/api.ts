/** Alineado con `NegocioResponse` de la API. */
export interface NegocioPublico {
  id: string
  slug: string
  nombre: string
  descripcionCorta?: string | null
  logoUrl?: string | null
  activo: boolean
  creadoEn: string
  /** Solo en respuesta de alta con dueño. */
  adminTiendaCreado?: boolean
  adminTiendaEmail?: string | null
}

/** Body de `POST /api/negocios` (SuperAdmin). */
export interface CrearNegocioPayload {
  slug: string
  nombre: string
  descripcionCorta?: string | null
  logoUrl?: string | null
  emailContacto?: string | null
  /** Opcional: crear usuario AdminTienda en la misma operación. */
  tiendaAdminEmail?: string | null
  tiendaAdminPassword?: string | null
  tiendaAdminNombre?: string | null
  tiendaAdminApellido?: string | null
}

/** Alineado con `CategoriaResponse` (público, solo activas). */
export interface CategoriaPublico {
  id: string
  negocioId: string
  nombre: string
  orden: number
  activo: boolean
  creadoEn: string
}

/** Respuesta paginada de listados admin. */
export interface PaginaResponseDto<T> {
  items: T[]
  pagina: number
  tamano: number
  total: number
  totalPaginas: number
}

/** Alineado con `ProductoResponse` (público, solo activos). */
export interface ProductoPublico {
  id: string
  negocioId: string
  categoriaId?: string | null
  nombre: string
  descripcionCorta?: string | null
  /** URL pública de la imagen (opcional). */
  imagenUrl?: string | null
  precio: number
  stock: number
  atributos?: Record<string, string> | null
  activo: boolean
  creadoEn: string
}

/** Respuesta de `POST .../pedidos` (campos usados en el front). */
export interface PedidoCreadoDto {
  id: string
  estado: string
  total: number
}

/** Respuesta de `POST .../mercadopago/preferencia`. */
export interface PreferenciaMercadoPagoDto {
  pedidoId: string
  preferenciaId: string
  urlPago: string
  urlPagoSandbox?: string | null
}

/** `UsuarioPublico` de la API. */
export interface UsuarioPublicoDto {
  id: string
  negocioId?: string | null
  /** Slug del negocio del usuario (AdminTienda / Cliente con negocio). */
  negocioSlug?: string | null
  email: string
  nombre: string
  apellido?: string | null
  rol: string
}

/** `AuthResponse` de `POST /api/auth/login`. */
export interface AuthResponseDto {
  token: string
  expiraEn: string
  usuario: UsuarioPublicoDto
}

export interface PedidoLineaListDto {
  productoId: string
  nombre: string
  cantidad: number
  precioUnitario: number
  subtotal: number
}

/** `PedidoResponse` en listados admin. */
export interface PedidoEstadoConteoDto {
  estado: string
  cantidad: number
}

export interface VentaPorDiaDto {
  fecha: string
  cantidadPedidos: number
  montoTotal: number
}

export interface VentasResumenDto {
  pedidosPorEstado: PedidoEstadoConteoDto[]
  montoTotalVentana: number
  pedidosPagadosVentana: number
  unidadesVendidasVentana: number
  pedidosPorEntregar: number
  ventasPorDia: VentaPorDiaDto[]
  ticketPromedioVentana: number
  productosTop: ProductoTopVentaDto[]
}

export interface ProductoTopVentaDto {
  productoId: string
  nombre: string
  cantidadVendida: number
  montoTotal: number
}

export interface PedidoNovedadesDto {
  pedidosPagadosNuevos: number
}

/** Estados que el admin puede asignar manualmente (post-pago). */
export const ESTADOS_PEDIDO_ADMIN = [
  { valor: 'EnPreparacion', etiqueta: 'En preparación' },
  { valor: 'Enviado', etiqueta: 'Enviado' },
  { valor: 'Entregado', etiqueta: 'Entregado' },
  { valor: 'Cancelado', etiqueta: 'Cancelado' },
] as const

export function pedidoAdminPuedeGestionar(estado: string) {
  return ['Pagado', 'Confirmado', 'EnPreparacion', 'Enviado'].includes(estado)
}

export function etiquetaEstadoPedido(estado: string) {
  const map: Record<string, string> = {
    PendientePago: 'Pendiente de pago',
    ProcesandoPago: 'Procesando pago',
    Pagado: 'Pagado',
    Rechazado: 'Pago rechazado',
    Confirmado: 'Confirmado',
    EnPreparacion: 'En preparación',
    Enviado: 'Enviado',
    Entregado: 'Entregado',
    Cancelado: 'Cancelado',
  }
  return map[estado] ?? estado
}

export const PASOS_SEGUIMIENTO_PEDIDO = [
  { clave: 'Pagado', etiqueta: 'Pagado' },
  { clave: 'EnPreparacion', etiqueta: 'Preparando' },
  { clave: 'Enviado', etiqueta: 'En camino' },
  { clave: 'Entregado', etiqueta: 'Entregado' },
] as const

/** Índice del paso actual en el seguimiento post-pago (-1 si no aplica). */
export function indiceSeguimientoPedido(estado: string): number {
  if (estado === 'Confirmado') return 0
  return PASOS_SEGUIMIENTO_PEDIDO.findIndex((p) => p.clave === estado)
}

export function pedidoMuestraSeguimiento(estado: string) {
  return indiceSeguimientoPedido(estado) >= 0
}

export function pedidoDebeAutoActualizar(estado: string) {
  return !['Entregado', 'Rechazado', 'Cancelado'].includes(estado)
}

export type ClaseEstadoPedidoCliente = 'ok' | 'err' | 'pending' | 'progress' | 'neutral'

export function claseEstadoPedidoCliente(estado: string): ClaseEstadoPedidoCliente {
  if (estado === 'Entregado') return 'ok'
  if (estado === 'Pagado' || estado === 'Confirmado') return 'ok'
  if (estado === 'EnPreparacion' || estado === 'Enviado') return 'progress'
  if (estado === 'Rechazado' || estado === 'Cancelado') return 'err'
  if (estado === 'PendientePago' || estado === 'ProcesandoPago') return 'pending'
  return 'neutral'
}

/** `ProductoResponse` admin (incluye inactivos). */
export interface ProductoAdminDto {
  id: string
  negocioId: string
  categoriaId?: string | null
  nombre: string
  descripcionCorta?: string | null
  imagenUrl?: string | null
  precio: number
  stock: number
  atributos?: Record<string, string> | null
  activo: boolean
  creadoEn: string
}

export interface CategoriaAdminDto {
  id: string
  negocioId: string
  nombre: string
  orden: number
  activo: boolean
  creadoEn: string
}

/** `GET /api/mis-pedidos` */
export interface PedidoClienteListItemDto {
  id: string
  negocioId: string
  negocioSlug: string
  negocioNombre: string
  estado: string
  total: number
  creadoEn: string
  lineas: PedidoLineaListDto[]
}

export interface PedidoListDto {
  id: string
  negocioId: string
  estado: string
  mercadoPagoPreferenceId?: string | null
  mercadoPagoPaymentId?: string | null
  mercadoPagoStatusDetail?: string | null
  lineas: PedidoLineaListDto[]
  total: number
  clienteNombre?: string | null
  clienteEmail?: string | null
  clienteTelefono?: string | null
  creadoEn: string
}
