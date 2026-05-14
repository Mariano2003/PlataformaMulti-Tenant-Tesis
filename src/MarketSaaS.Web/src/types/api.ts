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
  ventasPorDia: VentaPorDiaDto[]
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
