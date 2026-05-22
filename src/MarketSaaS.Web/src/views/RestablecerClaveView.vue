<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { apiUrl } from '../config/api'

const route = useRoute()
const router = useRouter()

const token = ref('')
const pwd = ref('')
const pwd2 = ref('')
const enviando = ref(false)
const error = ref<string | null>(null)
const exito = ref(false)

function leerToken(): string {
  const param = route.params.token
  if (typeof param === 'string' && param.trim()) return param.trim().toLowerCase()

  const q = route.query.token
  if (typeof q === 'string' && q.trim()) return q.trim().toLowerCase()
  if (Array.isArray(q) && q[0]) return String(q[0]).trim().toLowerCase()

  const hash = window.location.hash
  const qIdx = hash.indexOf('?')
  if (qIdx >= 0) {
    const t = new URLSearchParams(hash.slice(qIdx + 1)).get('token')
    if (t?.trim()) return t.trim().toLowerCase()
  }

  const match = hash.match(/\/restablecer-clave\/([a-fA-F0-9]{64})/i)
  if (match?.[1]) return match[1].toLowerCase()

  return ''
}

function sincronizarToken() {
  token.value = leerToken()
}

watch(() => route.fullPath, sincronizarToken, { immediate: true })

const tieneToken = computed(() => token.value.length === 64)

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
  if (!tieneToken.value) {
    error.value = 'El enlace no es válido. Solicitá uno nuevo.'
    return
  }

  enviando.value = true
  try {
    const res = await fetch(apiUrl('/api/auth/restablecer-clave'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        token: token.value,
        nuevaPassword: pwd.value,
      }),
    })
    const text = await res.text()
    let j: { mensaje?: string; error?: string } = {}
    try {
      j = JSON.parse(text) as { mensaje?: string; error?: string }
    } catch {
      /* no JSON */
    }
    if (!res.ok) {
      error.value = j.error ?? `Error ${res.status}`
      return
    }
    exito.value = true
    setTimeout(() => {
      void router.replace({ name: 'portal-login' })
    }, 2800)
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

        <p v-if="exito" class="portal-msg portal-msg--ok">
          Contraseña actualizada. En unos segundos te llevamos al acceso para que entres con la
          <strong>nueva</strong> contraseña.
        </p>

        <template v-else>
          <p v-if="!tieneToken" class="portal-msg portal-msg--error">
            Falta o está incompleto el enlace del correo. Pedí uno nuevo en
            <RouterLink :to="{ name: 'recuperar-clave' }">Olvidé mi contraseña</RouterLink>
            y abrí el mail desde el celular o PC (copiá el enlace completo si hace falta).
          </p>
          <template v-else>
            <p class="portal-auth__lead">Elegí una contraseña nueva (mínimo 8 caracteres).</p>
            <form class="portal-form" @submit.prevent="enviar">
              <label class="portal-field">
                <span>Nueva contraseña</span>
                <input
                  v-model="pwd"
                  type="password"
                  required
                  minlength="8"
                  autocomplete="new-password"
                />
              </label>
              <label class="portal-field">
                <span>Repetir contraseña</span>
                <input
                  v-model="pwd2"
                  type="password"
                  required
                  minlength="8"
                  autocomplete="new-password"
                />
              </label>
              <p v-if="error" class="portal-msg portal-msg--error">{{ error }}</p>
              <button type="submit" class="btn-primary" :disabled="enviando">
                {{ enviando ? 'Guardando…' : 'Guardar contraseña' }}
              </button>
            </form>
          </template>
        </template>

        <p class="portal-auth__links">
          <RouterLink :to="{ name: 'portal-login' }">← Ir al acceso</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>
