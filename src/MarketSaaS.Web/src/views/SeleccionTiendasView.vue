<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { apiUrl } from '../config/api'
import { useAuthStore } from '../stores/auth'
import type { NegocioPublico, UsuarioPublicoDto } from '../types/api'

const auth = useAuthStore()

const esSuperAdmin = computed(() => auth.usuario?.rol === 'SuperAdmin')

const textoSubtitulo = computed(() => {
  if (esSuperAdmin.value) {
    return 'Como SuperAdmin también podés entrar al panel de cualquier tienda. Para dar de alta una nueva, usá Plataforma.'
  }
  return 'Solo aparecen tiendas activas. Elegí una para ver el catálogo y comprar.'
})

const tiendas = ref<NegocioPublico[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

function nombreMostrar(u: UsuarioPublicoDto | null | undefined) {
  const base = u?.nombre?.trim()
  return base || u?.email || 'Usuario'
}

onMounted(async () => {
  loading.value = true
  error.value = null
  try {
    const res = await fetch(apiUrl('/api/negocios'))
    if (!res.ok) {
      error.value = `No se pudo cargar el catálogo (${res.status}).`
      return
    }
    tiendas.value = (await res.json()) as NegocioPublico[]
  } catch {
    error.value = 'No se pudo conectar con la API.'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <main class="pick-page">
    <header class="pick-hero pick-hero__row">
      <div>
        <h1>Elegí una tienda</h1>
        <p class="pick__sub">
          {{ textoSubtitulo }}
        </p>
      </div>
      <div v-if="auth.usuario" class="pick__user">
        <span class="pick__hello">Hola, {{ nombreMostrar(auth.usuario) }}</span>
        <RouterLink
          v-if="esSuperAdmin"
          class="pick__plataforma"
          :to="{ name: 'superadmin-plataforma' }"
        >
          Panel plataforma
        </RouterLink>
        <button type="button" class="pick__out" @click="auth.cerrarSesion()">Salir</button>
      </div>
    </header>

    <p v-if="loading" class="pick__state">Cargando tiendas…</p>
    <p v-else-if="error" class="pick__state pick__state--err">{{ error }}</p>
    <p v-else-if="!tiendas.length" class="pick__state">Todavía no hay tiendas activas.</p>

    <ul v-else class="pick-grid">
      <li v-for="n in tiendas" :key="n.id">
        <RouterLink class="pick-card" :to="{ name: 'tienda', params: { slug: n.slug } }">
          <span class="pick-card__name">{{ n.nombre }}</span>
          <span class="pick-card__badge">Entrar a la tienda →</span>
        </RouterLink>
      </li>
    </ul>

    <p class="pick__foot">
      Para usar otra cuenta, tocá <strong>Salir</strong> y volvé a ingresar desde Acceso.
    </p>
  </main>
</template>

<style scoped>
.pick-hero__row {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
}
.pick__sub {
  margin: 0;
  max-width: 36rem;
  line-height: 1.5;
  color: var(--text);
  font-size: 0.95rem;
}
.pick__user {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.35rem;
}
.pick__hello {
  font-size: 0.9rem;
  color: var(--text-h);
  font-weight: 600;
}
.pick__plataforma {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--primary-dark);
  text-decoration: none;
}
.pick__plataforma:hover {
  text-decoration: underline;
}
.pick__out {
  font-size: 0.85rem;
  padding: 0.35rem 0.75rem;
  border-radius: 999px;
  border: 1px solid var(--border-strong);
  background: var(--surface);
  cursor: pointer;
}
.pick__out:hover {
  border-color: var(--accent);
  color: var(--accent-dark);
}
.pick__state {
  margin: 1rem 0;
  color: var(--text-muted);
}
.pick__state--err {
  color: #b91c1c;
}
.pick__foot {
  margin-top: 2rem;
  font-size: 0.9rem;
  color: var(--text-muted);
}
</style>
