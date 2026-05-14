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
  <div class="portal-login">
    <nav class="portal-login__nav">
      <RouterLink to="/">Inicio</RouterLink>
    </nav>

    <div class="portal-login__card">
      <h1>Ingresar</h1>
      <p class="portal-login__lead">
        Según tu rol vas al lugar indicado: <strong>Cliente</strong> elige tienda para comprar,
        <strong>AdminTienda</strong> entra al panel de su negocio, <strong>SuperAdmin</strong> al panel de la
        plataforma (altas de tiendas).
      </p>

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

      <p class="portal-login__forgot">
        <RouterLink :to="{ name: 'recuperar-clave' }">Olvidé mi contraseña</RouterLink>
      </p>

      <p class="portal-login__hint">
        Acceso directo al panel de una tienda (sin pasar por aquí):
        <code>/admin/tu-slug/login</code>.
      </p>
    </div>
  </div>
</template>

<style scoped>
.portal-login {
  padding: 1.5rem 1.25rem 3rem;
  max-width: 26rem;
  margin: 0 auto;
  text-align: left;
}
.portal-login__nav {
  font-size: 0.9rem;
  margin-bottom: 1rem;
}
.portal-login__nav a {
  color: var(--accent, #2563eb);
  text-decoration: none;
}
.portal-login__nav a:hover {
  text-decoration: underline;
}
.sep {
  margin: 0 0.35rem;
  color: var(--text-muted, #9ca3af);
}
.portal-login__card {
  padding: 1.5rem;
  border-radius: 12px;
  border: 1px solid var(--border, #e5e7eb);
  background: var(--surface, #fff);
}
h1 {
  margin: 0 0 0.5rem;
  font-size: 1.35rem;
}
.portal-login__lead {
  margin: 0 0 1.25rem;
  line-height: 1.55;
  color: var(--text, #374151);
  font-size: 0.95rem;
}
.portal-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.portal-field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  font-size: 0.9rem;
}
.portal-field input {
  padding: 0.5rem 0.65rem;
  border-radius: 8px;
  border: 1px solid var(--border, #d1d5db);
}
.portal-msg--error {
  color: #b91c1c;
  font-size: 0.9rem;
  margin: 0;
}
.btn-primary {
  padding: 0.55rem 1rem;
  border-radius: 8px;
  border: none;
  background: var(--accent, #2563eb);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}
.btn-primary:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}
.portal-login__forgot {
  margin: 0.85rem 0 0;
  font-size: 0.9rem;
}
.portal-login__forgot a {
  color: var(--accent, #2563eb);
  text-decoration: none;
}
.portal-login__forgot a:hover {
  text-decoration: underline;
}
.portal-login__hint {
  margin: 1.25rem 0 0;
  font-size: 0.85rem;
  color: var(--text-muted, #6b7280);
  line-height: 1.45;
}
.portal-login__hint a {
  color: var(--accent, #2563eb);
}
code {
  font-size: 0.8rem;
}
</style>
