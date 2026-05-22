<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink } from 'vue-router'
import { apiUrl } from '../config/api'

const email = ref('')
const enviando = ref(false)
const mensaje = ref<string | null>(null)
const error = ref<string | null>(null)

async function enviar() {
  mensaje.value = null
  error.value = null
  enviando.value = true
  try {
    const res = await fetch(apiUrl('/api/auth/recuperar-clave'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: email.value.trim().toLowerCase() }),
    })
    const text = await res.text()
    let j: { mensaje?: string; error?: string } = {}
    try {
      j = JSON.parse(text) as { mensaje?: string; error?: string }
    } catch {
      /* body no JSON */
    }
    if (!res.ok) {
      error.value = j.error ?? `Error ${res.status}`
      return
    }
    mensaje.value =
      j.mensaje ??
      'Si el correo está registrado, revisá tu bandeja (y spam).'
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
        <h1>Olvidé mi contraseña</h1>
        <p class="portal-auth__lead">
          Te enviamos un enlace al correo si está registrado y el servidor tiene configurado Gmail (SMTP).
          En desarrollo tenés que activar <code>Email:Enabled</code> y las credenciales en la API.
        </p>
        <form class="portal-form" @submit.prevent="enviar">
          <label class="portal-field">
            <span>Email</span>
            <input v-model="email" type="email" required autocomplete="username" />
          </label>
          <p v-if="error" class="portal-msg portal-msg--error">{{ error }}</p>
          <p v-if="mensaje" class="portal-msg portal-msg--ok">{{ mensaje }}</p>
          <button type="submit" class="btn-primary" :disabled="enviando">
            {{ enviando ? 'Enviando…' : 'Enviar enlace' }}
          </button>
        </form>
        <p class="portal-auth__links">
          <RouterLink :to="{ name: 'portal-login' }">← Volver al acceso</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>
