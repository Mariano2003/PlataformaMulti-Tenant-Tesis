import type { ProductoAdminDto, ProductoPublico } from '../types/api'

/** Acepta camelCase o PascalCase por si el JSON de la API varía. */
function imagenUrlDesdeObjeto(o: Record<string, unknown>): string | null {
  const v = o.imagenUrl ?? o.ImagenUrl ?? o.imagen_url
  if (typeof v !== 'string') return null
  const t = v.trim()
  return t.length > 0 ? t : null
}

export function normalizarProductoAdminDto(raw: Record<string, unknown>): ProductoAdminDto {
  return {
    ...(raw as unknown as ProductoAdminDto),
    imagenUrl: imagenUrlDesdeObjeto(raw),
  }
}

export function normalizarProductoPublicoDto(raw: Record<string, unknown>): ProductoPublico {
  return {
    ...(raw as unknown as ProductoPublico),
    imagenUrl: imagenUrlDesdeObjeto(raw),
  }
}
