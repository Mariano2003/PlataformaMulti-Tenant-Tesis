<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink, RouterView, useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth'

const route = useRoute()
const auth = useAuthStore()
const esAdmin = computed(() => route.path.startsWith('/admin') && route.name !== 'admin-login')
const esAuthPortal = computed(() => {
  const n = route.name
  return (
    n === 'portal-login' ||
    n === 'portal-registro' ||
    n === 'recuperar-clave' ||
    n === 'restablecer-clave' ||
    n === 'admin-login'
  )
})
const enlacesNav = computed(() => {
  if (!auth.token || !auth.usuario) {
    return [{ label: 'Acceso', to: { name: 'portal-login' as const } }]
  }
  const rol = auth.usuario.rol
  if (rol === 'SuperAdmin') {
    return [
      { label: 'Plataforma', to: { name: 'superadmin-plataforma' as const } },
      { label: 'Tiendas', to: { name: 'tiendas' as const } },
      { label: 'Mis pedidos', to: { name: 'mis-pedidos' as const } },
    ]
  }
  if (rol === 'Cliente') {
    return [
      { label: 'Mis tiendas', to: { name: 'tiendas' as const } },
      { label: 'Mis pedidos', to: { name: 'mis-pedidos' as const } },
    ]
  }
  return []
})
const mostrarNavPortal = computed(
  () => !esAdmin.value && !esAuthPortal.value && enlacesNav.value.length > 0,
)
</script>

<template>
  <div
    class="shell"
    :class="{
      'shell--admin': esAdmin,
      'shell--auth': esAuthPortal,
      'shell--store': !esAdmin && !esAuthPortal,
    }"
  >
    <header v-if="!esAuthPortal" class="bar" :class="{ 'bar--admin': esAdmin }">
      <RouterLink to="/" class="brand">
        Market<span class="brand-accent">SaaS</span>
      </RouterLink>
      <span v-if="esAdmin" class="bar-badge">Panel tienda</span>
      <span v-else-if="auth.usuario?.rol === 'SuperAdmin'" class="bar-badge bar-badge--super"
        >SuperAdmin</span
      >
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
    <main class="shell-main" :class="{ 'shell-main--store': !esAdmin && !esAuthPortal }">
      <RouterView />
    </main>
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
.shell-main {
  flex: 1;
  display: flex;
  flex-direction: column;
}
.shell-main--store {
  background: linear-gradient(180deg, var(--bg-soft) 0%, var(--bg) 28%);
}
.shell--auth {
  min-height: 100svh;
}
.bar {
  padding: 0.7rem clamp(1rem, 4vw, 1.5rem);
  border-bottom: none;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: linear-gradient(90deg, var(--hero-from) 0%, var(--hero-to) 100%);
  box-shadow: var(--shadow-sm);
}
.bar--admin {
  background: var(--surface);
  border-bottom: 1px solid var(--border);
  box-shadow: none;
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
  font-weight: 600;
  color: #fff;
  text-decoration: none;
  padding: 0.35rem 0.75rem;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.15);
}
.bar-link:hover {
  background: rgba(255, 255, 255, 0.28);
  text-decoration: none;
}
.bar--admin .bar-link {
  color: var(--primary-dark);
  background: var(--primary-light);
}
.bar--admin .bar-link:hover {
  background: #99f6e4;
}
.brand {
  font-weight: 800;
  font-size: 1.2rem;
  color: #fff;
  text-decoration: none;
  letter-spacing: -0.03em;
}
.bar--admin .brand {
  color: var(--text-h);
}
.brand-accent {
  color: #fde68a;
}
.bar--admin .brand-accent {
  color: var(--accent);
}
.bar-badge {
  font-size: 0.68rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  padding: 0.2rem 0.5rem;
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.2);
  color: #fff;
  border: 1px solid rgba(255, 255, 255, 0.35);
}
.bar--admin .bar-badge {
  background: var(--accent-light);
  color: var(--accent-dark);
  border-color: var(--accent-border);
}
.bar-badge--super {
  background: rgba(255, 255, 255, 0.25);
  color: #ede9fe;
  border-color: rgba(255, 255, 255, 0.4);
}
.bar--admin .bar-badge--super {
  background: #ede9fe;
  color: #5b21b6;
  border-color: #c4b5fd;
}
</style>
