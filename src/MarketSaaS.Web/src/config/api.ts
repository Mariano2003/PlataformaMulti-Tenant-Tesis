/**
 * URL base de la API. En desarrollo, dejala vacía: Vite reenvía `/api` al backend (vite.config.ts).
 * En producción: `VITE_API_BASE_URL` sin `/api` al final (ej. https://api.tudominio.com).
 */
function normalizeApiBase(raw: string | undefined): string {
  if (!raw) return '';
  let base = raw.trim().replace(/\/$/, '');
  // Evita /api/api/auth/... si copiaron la URL con sufijo /api → 404 en el servidor.
  if (/\/api$/i.test(base)) base = base.slice(0, -4).replace(/\/$/, '');
  return base;
}

export function apiUrl(path: string): string {
  const base = normalizeApiBase(import.meta.env.VITE_API_BASE_URL as string | undefined);
  const p = path.startsWith('/') ? path : `/${path}`;
  return base ? `${base}${p}` : p;
}
