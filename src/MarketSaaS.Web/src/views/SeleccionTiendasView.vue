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
  <main class="pick">
    <header class="pick__head">
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

    <ul v-else class="pick__grid">
      <li v-for="n in tiendas" :key="n.id" class="pick__card-wrap">
        <RouterLink class="pick__card" :to="{ name: 'tienda', params: { slug: n.slug } }">
          <span class="pick__nombre">{{ n.nombre }}</span>
          <span v-if="n.descripcionCorta" class="pick__desc">{{ n.descripcionCorta }}</span>
          <span class="pick__slug">{{ n.slug }}</span>
        </RouterLink>
      </li>
    </ul>

    <p class="pick__foot">
      Para usar otra cuenta, tocá <strong>Salir</strong> y volvé a ingresar desde Acceso.
    </p>
  </main>
</template>

<style scoped>
.pick {
  padding: 1.5rem 1.25rem 3rem;
  max-width: 56rem;
  margin: 0 auto;
  text-align: left;
}
.pick__head {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  margin-bottom: 1.5rem;
}
h1 {
  margin: 0 0 0.35rem;
  font-size: 1.65rem;
}
.pick__sub {
  margin: 0;
  max-width: 36rem;
  line-height: 1.5;
  color: var(--text, #4b5563);
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
  color: var(--text, #374151);
}
.pick__plataforma {
  font-size: 0.85rem;
  font-weight: 500;
  color: var(--accent, #2563eb);
  text-decoration: none;
}
.pick__plataforma:hover {
  text-decoration: underline;
}
.pick__out {
  font-size: 0.85rem;
  padding: 0.35rem 0.65rem;
  border-radius: 8px;
  border: 1px solid var(--border, #d1d5db);
  background: transparent;
  cursor: pointer;
}
.pick__out:hover {
  background: var(--code-bg, #f3f4f6);
}
.pick__state {
  margin: 1rem 0;
  color: var(--text-muted, #6b7280);
}
.pick__state--err {
  color: #b91c1c;
}
.pick__grid {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 1rem;
  grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr));
}
.pick__card {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  padding: 1rem;
  border-radius: 12px;
  border: 1px solid var(--accent-border, rgba(37, 99, 235, 0.35));
  background: var(--accent-bg, rgba(37, 99, 235, 0.06));
  text-decoration: none;
  color: inherit;
  min-height: 5rem;
  transition: background 0.15s ease;
}
.pick__card:hover {
  background: rgba(37, 99, 235, 0.12);
}
.pick__nombre {
  font-weight: 600;
  font-size: 1.05rem;
  color: var(--text-h, #111827);
}
.pick__desc {
  font-size: 0.85rem;
  color: var(--text, #4b5563);
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.pick__slug {
  margin-top: auto;
  font-size: 0.75rem;
  font-family: ui-monospace, monospace;
  color: var(--text-muted, #6b7280);
}
.pick__foot {
  margin-top: 2rem;
  font-size: 0.9rem;
  color: var(--text-muted, #6b7280);
}
.pick__foot a {
  color: var(--accent, #2563eb);
}
</style>
