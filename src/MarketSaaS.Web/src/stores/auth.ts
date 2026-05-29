import { defineStore } from 'pinia'
import { computed, ref, watch } from 'vue'
import type { AuthResponseDto, UsuarioPublicoDto } from '../types/api'

const STORAGE_KEY = 'marketsaas.auth.v1'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(null)
  const usuario = ref<UsuarioPublicoDto | null>(null)

  const estaAutenticado = computed(() => Boolean(token.value))

  function persistir() {
    if (typeof localStorage === 'undefined') return
    try {
      if (!token.value) {
        localStorage.removeItem(STORAGE_KEY)
        return
      }
      localStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({
          token: token.value,
          usuario: usuario.value,
        }),
      )
    } catch {
      /* quota */
    }
  }

  function hidratar() {
    if (typeof localStorage === 'undefined') return
    try {
      const raw = localStorage.getItem(STORAGE_KEY)
      if (!raw) return
      const data = JSON.parse(raw) as {
        token?: string
        usuario?: UsuarioPublicoDto
      }
      if (typeof data.token === 'string' && data.token) token.value = data.token
      if (data.usuario) usuario.value = data.usuario
    } catch {
      localStorage.removeItem(STORAGE_KEY)
    }
  }

  function setSesion(respuesta: AuthResponseDto) {
    token.value = respuesta.token
    usuario.value = respuesta.usuario
  }

  function cerrarSesion() {
    token.value = null
    usuario.value = null
    void import('../router').then(({ router }) => {
      void router.replace({ name: 'portal-login' })
    })
  }

  hidratar()

  watch([token, usuario], persistir, { deep: true })

  return {
    token,
    usuario,
    estaAutenticado,
    setSesion,
    cerrarSesion,
  }
})
