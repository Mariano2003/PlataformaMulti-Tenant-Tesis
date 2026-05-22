<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { apiUrl } from '../config/api'

const route = useRoute()
const router = useRouter()

const token = ref('')
const pwd = ref('')
const pwd2 = ref('')
const enviando = ref(false)
const error = ref<string | null>(null)

const tieneToken = computed(() => token.value.trim().length > 0)

onMounted(() => {
  const q = route.query.token
  token.value = typeof q === 'string' ? q.trim() : ''
})

async function enviar() {
  error.value = null
  if (pwd.value !== pwd2.value) {
    error.value = 'Las contraseñas no coinciden.'
    return
  }
  if (pwd.value.length < 8) {
    error.value = 'La contraseña debe tener al menos 8 caracteres.'
    return
  }
  enviando.value = true
  try {
    const res = await fetch(apiUrl('/api/auth/restablecer-clave'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        token: token.value.trim(),
        nuevaPassword: pwd.value,
      }),
    })
    const text = await res.text()
    try {
      const j = JSON.parse(text) as { mensaje?: string; error?: string }
      if (!res.ok) {
        error.value = j.error ?? `Error ${res.status}`
        return
      }
      await router.replace({ name: 'portal-login' })
    } catch {
      error.value = !res.ok ? `Error ${res.status}` : 'Respuesta inválida.'
    }
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
      <RouterLink :to="{ name: 'portal-login' }" class="portal-auth__brand">Market<span>SaaS</span></RouterLink>

      <div class="portal-auth__card">
        <h1>Nueva contraseña</h1>
        <p v-if="!tieneToken" class="portal-msg portal-msg--error">
          Falta el token en la URL. Abrí el enlace que te llegó por correo o
          <RouterLink :to="{ name: 'recuperar-clave' }">solicitá uno nuevo</RouterLink>.
        </p>
        <template v-else>
          <p class="portal-auth__lead">Elegí una contraseña nueva (mínimo 8 caracteres).</p>
          <form class="portal-form" @submit.prevent="enviar">
            <label class="portal-field">
              <span>Nueva contraseña</span>
              <input v-model="pwd" type="password" required minlength="8" autocomplete="new-password" />
            </label>
            <label class="portal-field">
              <span>Repetir contraseña</span>
              <input v-model="pwd2" type="password" required minlength="8" autocomplete="new-password" />
            </label>
            <p v-if="error" class="portal-msg portal-msg--error">{{ error }}</p>
            <button type="submit" class="btn-primary" :disabled="enviando">
              {{ enviando ? 'Guardando…' : 'Guardar y entrar' }}
            </button>
          </form>
        </template>
        <p class="portal-auth__links">
          <RouterLink :to="{ name: 'portal-login' }">← Ir al acceso</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>
