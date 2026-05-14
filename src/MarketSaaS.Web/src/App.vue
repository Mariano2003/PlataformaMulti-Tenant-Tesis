<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, RouterView, useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth'

const route = useRoute()
const auth = useAuthStore()
const esAdmin = computed(() => route.path.startsWith('/admin'))
const esLoginPortal = computed(() => route.path === '/acceder')
const enlacesNav = computed(() => {
  if (!auth.token || !auth.usuario) {
    return [{ label: 'Acceso', to: { name: 'portal-login' as const } }]
  }
  const rol = auth.usuario.rol
  if (rol === 'SuperAdmin') {
    return [
      { label: 'Plataforma', to: { name: 'superadmin-plataforma' as const } },
      { label: 'Tiendas', to: { name: 'tiendas' as const } },
    ]
  }
  if (rol === 'Cliente') {
    return [{ label: 'Mis tiendas', to: { name: 'tiendas' as const } }]
  }
  return []
})
const mostrarNavPortal = computed(
  () => !esAdmin.value && !esLoginPortal.value && enlacesNav.value.length > 0,
)
</script>

<template>
  <div class="shell" :class="{ 'shell--admin': esAdmin }">
    <header class="bar">
      <RouterLink to="/" class="brand">MarketSaaS</RouterLink>
      <span v-if="esAdmin" class="bar-badge">Panel tienda</span>
      <span v-else-if="auth.usuario?.rol === 'SuperAdmin'" class="bar-badge bar-badge--super">SuperAdmin</span>
      <nav v-if="mostrarNavPortal" class="bar-nav">
        <RouterLink
          v-for="item in enlacesNav"
          :key="item.label"
          class="bar-link"
          :to="item.to"
        >
          {{ item.label }}
        </RouterLink>
      </nav>
    </header>
    <RouterView />
  </div>
</template>

<style scoped>
.shell {
  text-align: left;
  min-height: 100%;
  flex: 1;
  display: flex;
  flex-direction: column;
}
.bar {
  padding: 0.75rem 1.25rem;
  border-bottom: 1px solid var(--border, #e5e7eb);
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
.bar-nav {
  margin-left: auto;
  display: flex;
  flex-wrap: wrap;
  gap: 0.65rem 1rem;
  align-items: center;
}
.bar-link {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--accent, #2563eb);
  text-decoration: none;
}
.bar-link:hover {
  text-decoration: underline;
}
.shell--admin .bar {
  background: var(--code-bg);
}
.brand {
  font-weight: 600;
  font-size: 1.1rem;
  color: var(--text-h, #111827);
  text-decoration: none;
}
.brand:hover {
  color: var(--accent, #2563eb);
}
.bar-badge {
  font-size: 0.7rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  padding: 0.2rem 0.5rem;
  border-radius: 6px;
  background: var(--accent-bg);
  color: var(--accent);
  border: 1px solid var(--accent-border);
}
.bar-badge--super {
  background: rgba(124, 58, 237, 0.12);
  color: #6d28d9;
  border-color: rgba(124, 58, 237, 0.35);
}
</style>
