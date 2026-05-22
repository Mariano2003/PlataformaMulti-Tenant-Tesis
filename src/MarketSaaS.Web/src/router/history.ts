import { createWebHashHistory, createWebHistory } from 'vue-router'

/**
 * En hosts estáticos (Render, etc.) `/tienda/slug` devuelve 404 sin rewrite.
 * Con hash, el servidor solo sirve `/` y Vue lee `/#/tienda/slug` (retorno MP incluido).
 *
 * - Producción: hash por defecto.
 * - Desarrollo: history (URLs limpias), salvo VITE_ROUTER_HASH=true.
 * - Forzar history en prod: VITE_ROUTER_HASH=false (requiere rewrite /* → index.html).
 */
export function crearHistorialRouter() {
  const base = import.meta.env.BASE_URL
  const forzarHash = import.meta.env.VITE_ROUTER_HASH === 'true'
  const forzarHistory = import.meta.env.VITE_ROUTER_HASH === 'false'

  const usarHash =
    forzarHash || (import.meta.env.PROD && !forzarHistory)

  return usarHash ? createWebHashHistory(base) : createWebHistory(base)
}
