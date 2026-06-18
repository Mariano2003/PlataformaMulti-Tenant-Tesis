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
const enviando = ref(false)
const error = ref<string | null>(null)

async function enviar() {
  error.value = null
  enviando.value = true
  try {
    const res = await fetch(apiUrl('/api/auth/login'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: email.value.trim(),
        password: password.value,
      }),
    })
    if (!res.ok) {
      error.value =
        res.status === 401 ? 'Email o contraseña incorrectos.' : `Error ${res.status}`
      return
    }
    const data = (await res.json()) as AuthResponseDto
    auth.setSesion(data)

    const redirect = route.query.redirect as string | undefined
    if (redirect?.startsWith('/')) {
      await router.replace(redirect)
      return
    }

    const rol = data.usuario.rol
    const slugAdmin = data.usuario.negocioSlug?.trim()

    if (rol === 'SuperAdmin') {
      await router.replace({ name: 'superadmin-plataforma' })
      return
    }

    if (rol === 'AdminTienda' && slugAdmin) {
      await router.replace({
        name: 'admin-pedidos',
        params: { slug: slugAdmin },
      })
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
        <h1>Ingresar</h1>

        <form class="portal-form" @submit.prevent="enviar">
          <label class="portal-field">
            <span>Email</span>
            <input v-model="email" type="email" required autocomplete="username" />
          </label>
          <label class="portal-field">
            <span>Contraseña</span>
            <input
              v-model="password"
              type="password"
              required
              autocomplete="current-password"
            />
          </label>
          <p v-if="error" class="portal-msg portal-msg--error">{{ error }}</p>
          <button type="submit" class="btn-primary" :disabled="enviando">
            {{ enviando ? 'Entrando…' : 'Entrar' }}
          </button>
        </form>

        <p class="portal-auth__links">
          <RouterLink :to="{ name: 'portal-registro' }">Crear cuenta de cliente</RouterLink>
          <span class="portal-auth__sep">·</span>
          <RouterLink :to="{ name: 'recuperar-clave' }">Olvidé mi contraseña</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>
