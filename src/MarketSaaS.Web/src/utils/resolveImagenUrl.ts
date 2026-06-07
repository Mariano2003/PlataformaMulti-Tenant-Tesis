import { apiUrl } from '../config/api'

/** Convierte rutas relativas de uploads (`/uploads/...`) en URL absoluta de la API. */
export function resolveImagenUrl(url: string | null | undefined): string {
  if (!url) return ''
  const t = url.trim()
  if (!t) return ''
  if (t.startsWith('/uploads/')) return apiUrl(t)
  return t
}
