<script setup lang="ts">
import { computed, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { apiUrl } from '../../config/api'
import { useAuthStore } from '../../stores/auth'
import type { AuthResponseDto } from '../../types/api'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const slug = computed(() => (route.params.slug as string) || '')

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
    } else {
      await router.replace({
        name: 'admin-pedidos',
        params: { slug: slug.value },
      })
    }
  } catch {
    error.value = 'No se pudo conectar con la API.'
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="admin-login">
    <nav class="admin-breadcrumb">
      <RouterLink to="/">Inicio</RouterLink>
      <span class="sep">·</span>
      <RouterLink :to="{ name: 'tienda', params: { slug } }">Tienda pública</RouterLink>
    </nav>

    <div class="admin-login__card">
      <h1>Admin — {{ slug || '…' }}</h1>
      <p class="admin-login__lead">
        Ingresá con un usuario <strong>SuperAdmin</strong> o <strong>AdminTienda</strong> de este
        negocio.
      </p>

      <form class="admin-form admin-form--stretch" @submit.prevent="enviar">
        <label class="admin-field">
          <span>Email</span>
          <input v-model="email" type="email" required autocomplete="username" />
        </label>
        <label class="admin-field">
          <span>Contraseña</span>
          <input
            v-model="password"
            type="password"
            required
            autocomplete="current-password"
          />
        </label>
        <p v-if="error" class="admin-msg admin-msg--error admin-msg--compact">{{ error }}</p>
        <button type="submit" class="btn-primary" :disabled="enviando || !slug">
          {{ enviando ? 'Entrando…' : 'Entrar' }}
        </button>
      </form>
      <p class="admin-login__forgot">
        <RouterLink :to="{ name: 'recuperar-clave' }">Olvidé mi contraseña</RouterLink>
      </p>
    </div>
  </div>
</template>
