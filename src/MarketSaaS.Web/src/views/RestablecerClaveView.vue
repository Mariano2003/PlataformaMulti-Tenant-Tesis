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
  <div class="rst">
    <nav class="rst__nav">
      <RouterLink :to="{ name: 'portal-login' }">← Ir al acceso</RouterLink>
    </nav>
    <div class="rst__card">
      <h1>Nueva contraseña</h1>
      <p v-if="!tieneToken" class="rst__err">
        Falta el token en la URL. Abrí el enlace que te llegó por correo o
        <RouterLink :to="{ name: 'recuperar-clave' }">solicitá uno nuevo</RouterLink>.
      </p>
      <template v-else>
        <p class="rst__lead">Elegí una contraseña nueva (mínimo 8 caracteres).</p>
        <form class="rst__form" @submit.prevent="enviar">
          <label class="rst__field">
            <span>Nueva contraseña</span>
            <input v-model="pwd" type="password" required minlength="8" autocomplete="new-password" />
          </label>
          <label class="rst__field">
            <span>Repetir contraseña</span>
            <input v-model="pwd2" type="password" required minlength="8" autocomplete="new-password" />
          </label>
          <p v-if="error" class="rst__err">{{ error }}</p>
          <button type="submit" class="rst__btn" :disabled="enviando">
            {{ enviando ? 'Guardando…' : 'Guardar y entrar' }}
          </button>
        </form>
      </template>
    </div>
  </div>
</template>

<style scoped>
.rst {
  padding: 1.5rem 1.25rem 3rem;
  max-width: 26rem;
  margin: 0 auto;
  text-align: left;
}
.rst__nav {
  margin-bottom: 1rem;
  font-size: 0.9rem;
}
.rst__nav a {
  color: var(--accent, #2563eb);
  text-decoration: none;
}
.rst__card {
  padding: 1.5rem;
  border-radius: 12px;
  border: 1px solid var(--border, #e5e7eb);
}
h1 {
  margin: 0 0 0.75rem;
  font-size: 1.35rem;
}
.rst__lead {
  margin: 0 0 1rem;
  font-size: 0.95rem;
  color: var(--text, #374151);
}
.rst__form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.rst__field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  font-size: 0.9rem;
}
.rst__field input {
  padding: 0.5rem 0.65rem;
  border-radius: 8px;
  border: 1px solid var(--border, #d1d5db);
}
.rst__err {
  margin: 0;
  color: #b91c1c;
  font-size: 0.9rem;
}
.rst__err a {
  color: var(--accent, #2563eb);
}
.rst__btn {
  padding: 0.55rem 1rem;
  border-radius: 8px;
  border: none;
  background: var(--accent, #2563eb);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}
.rst__btn:disabled {
  opacity: 0.65;
}
</style>
