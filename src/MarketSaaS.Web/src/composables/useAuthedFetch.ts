import { apiUrl } from '../config/api'
import { useAuthStore } from '../stores/auth'

/** `fetch` a la API con `Authorization: Bearer` si hay sesión. */
export function useAuthedFetch() {
  const auth = useAuthStore()

  return function authedFetch(path: string, init: RequestInit = {}) {
    const headers = new Headers(init.headers)
    if (auth.token) headers.set('Authorization', `Bearer ${auth.token}`)
    return fetch(apiUrl(path), { ...init, headers })
  }
}
