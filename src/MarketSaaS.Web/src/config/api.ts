/**
 * URL base de la API. En desarrollo, dejala vacía: Vite reenvía `/api` al backend (vite.config.ts).
 * En build de producción, definí `VITE_API_BASE_URL` (ej. https://api.tudominio.com).
 */
export function apiUrl(path: string): string {
  const raw = import.meta.env.VITE_API_BASE_URL as string | undefined;
  const base = raw?.replace(/\/$/, '') ?? '';
  const p = path.startsWith('/') ? path : `/${path}`;
  return base ? `${base}${p}` : p;
}
