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
  <div class="rec">
    <nav class="rec__nav">
      <RouterLink :to="{ name: 'portal-login' }">← Volver al acceso</RouterLink>
    </nav>
    <div class="rec__card">
      <h1>Olvidé mi contraseña</h1>
      <p class="rec__lead">
        Te enviamos un enlace al correo si está registrado y el servidor tiene configurado Gmail (SMTP).
        En desarrollo tenés que activar <code>Email:Enabled</code> y las credenciales en la API.
      </p>
      <form class="rec__form" @submit.prevent="enviar">
        <label class="rec__field">
          <span>Email</span>
          <input v-model="email" type="email" required autocomplete="username" />
        </label>
        <p v-if="error" class="rec__err">{{ error }}</p>
        <p v-if="mensaje" class="rec__ok">{{ mensaje }}</p>
        <button type="submit" class="rec__btn" :disabled="enviando">
          {{ enviando ? 'Enviando…' : 'Enviar enlace' }}
        </button>
      </form>
    </div>
  </div>
</template>

<style scoped>
.rec {
  padding: 1.5rem 1.25rem 3rem;
  max-width: 26rem;
  margin: 0 auto;
  text-align: left;
}
.rec__nav {
  margin-bottom: 1rem;
  font-size: 0.9rem;
}
.rec__nav a {
  color: var(--accent, #2563eb);
  text-decoration: none;
}
.rec__card {
  padding: 1.5rem;
  border-radius: 12px;
  border: 1px solid var(--border, #e5e7eb);
}
h1 {
  margin: 0 0 0.5rem;
  font-size: 1.35rem;
}
.rec__lead {
  margin: 0 0 1.25rem;
  font-size: 0.9rem;
  line-height: 1.5;
  color: var(--text-muted, #6b7280);
}
.rec__form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.rec__field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  font-size: 0.9rem;
}
.rec__field input {
  padding: 0.5rem 0.65rem;
  border-radius: 8px;
  border: 1px solid var(--border, #d1d5db);
}
.rec__err {
  margin: 0;
  color: #b91c1c;
  font-size: 0.9rem;
}
.rec__ok {
  margin: 0;
  color: #15803d;
  font-size: 0.9rem;
}
.rec__btn {
  padding: 0.55rem 1rem;
  border-radius: 8px;
  border: none;
  background: var(--accent, #2563eb);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}
.rec__btn:disabled {
  opacity: 0.65;
}
code {
  font-size: 0.8rem;
}
</style>
