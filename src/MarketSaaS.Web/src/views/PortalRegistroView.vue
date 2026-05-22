<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { apiUrl } from '../config/api'
import { useAuthStore } from '../stores/auth'
import type { AuthResponseDto } from '../types/api'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const nombre = ref('')
const apellido = ref('')
const telefono = ref('')
const enviando = ref(false)
const error = ref<string | null>(null)

async function leerErrorApi(res: Response): Promise<string> {
  try {
    const data = (await res.json()) as { error?: string }
    if (data?.error) return data.error
  } catch {
    /* ignore */
  }
  return res.status === 409 ? 'Ese email ya está registrado.' : `Error ${res.status}`
}

async function enviar() {
  error.value = null
  if (password.value.length < 8) {
    error.value = 'La contraseña debe tener al menos 8 caracteres.'
    return
  }
  enviando.value = true
  try {
    const res = await fetch(apiUrl('/api/auth/registro'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: email.value.trim(),
        password: password.value,
        nombre: nombre.value.trim(),
        apellido: apellido.value.trim() || undefined,
        telefono: telefono.value.trim() || undefined,
      }),
    })
    if (!res.ok) {
      error.value = await leerErrorApi(res)
      return
    }
    const data = (await res.json()) as AuthResponseDto
    auth.setSesion(data)

    const redirect = route.query.redirect as string | undefined
    if (redirect?.startsWith('/')) {
      await router.replace(redirect)
      return
    }
    await router.replace({ name: 'tiendas' })
  } catch {
    error.value = 'No se pudo conectar con la API.'
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="portal-auth">
    <div class="portal-auth__backdrop" aria-hidden="true" />

    <div class="portal-auth__inner">
      <RouterLink to="/" class="portal-auth__brand">Market<span>SaaS</span></RouterLink>

      <div class="portal-auth__card">
        <h1>Crear cuenta de cliente</h1>
        <p class="portal-auth__lead">
          Registrate para comprar en las tiendas de la plataforma. Si sos dueño de un negocio, tu cuenta
          la crea el administrador de la plataforma al dar de alta la tienda.
        </p>

        <form class="portal-form" @submit.prevent="enviar">
          <label class="portal-field">
            <span>Nombre</span>
            <input v-model="nombre" type="text" maxlength="100" required autocomplete="given-name" />
          </label>
          <label class="portal-field">
            <span>Apellido (opcional)</span>
            <input v-model="apellido" type="text" maxlength="100" autocomplete="family-name" />
          </label>
          <label class="portal-field">
            <span>Email</span>
            <input v-model="email" type="email" required autocomplete="email" />
          </label>
          <label class="portal-field">
            <span>Teléfono (opcional)</span>
            <input v-model="telefono" type="tel" maxlength="40" autocomplete="tel" />
          </label>
          <label class="portal-field">
            <span>Contraseña (mín. 8 caracteres)</span>
            <input
              v-model="password"
              type="password"
              required
              minlength="8"
              autocomplete="new-password"
            />
          </label>
          <p v-if="error" class="portal-msg portal-msg--error">{{ error }}</p>
          <button type="submit" class="btn-primary" :disabled="enviando">
            {{ enviando ? 'Creando cuenta…' : 'Registrarme' }}
          </button>
        </form>

        <p class="portal-auth__links">
          ¿Ya tenés cuenta?
          <RouterLink :to="{ name: 'portal-login' }">Iniciar sesión</RouterLink>
        </p>

        <p class="portal-auth__hint">
          Panel de una tienda (dueño):
          <code>/admin/tu-slug/login</code> — el dueño no se registra desde aquí.
        </p>
      </div>
    </div>
  </div>
</template>
