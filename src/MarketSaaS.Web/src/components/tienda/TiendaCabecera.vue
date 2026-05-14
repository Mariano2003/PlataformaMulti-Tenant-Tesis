<script setup lang="ts">
import { RouterLink } from 'vue-router'
import type { NegocioPublico } from '../../types/api'

defineProps<{
  negocio: NegocioPublico | null
  slug: string
  totalCarrito: number
}>()
</script>

<template>
  <header class="cabecera">
    <div class="titulos">
      <h1>{{ negocio?.nombre ?? 'Tienda' }}</h1>
      <p v-if="negocio?.descripcionCorta" class="desc">{{ negocio.descripcionCorta }}</p>
      <p class="meta">Slug: <code>{{ slug }}</code></p>
    </div>
    <RouterLink
      v-if="slug && totalCarrito > 0"
      class="btn-carrito"
      :to="{ name: 'checkout', params: { slug } }"
    >
      Carrito ({{ totalCarrito }})
    </RouterLink>
  </header>
</template>

<style scoped>
.cabecera {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.5rem;
}
.titulos {
  flex: 1;
  min-width: 12rem;
}
h1 {
  margin: 0 0 0.35rem;
  font-size: 1.5rem;
}
.desc {
  margin: 0 0 0.5rem;
  line-height: 1.45;
  color: var(--text, #4b5563);
}
.meta {
  margin: 0;
  font-size: 0.85rem;
  color: var(--text, #6b7280);
}
.meta code {
  font-size: 0.9em;
}
.btn-carrito {
  display: inline-block;
  padding: 0.45rem 0.9rem;
  border-radius: 8px;
  background: var(--accent-bg, rgba(37, 99, 235, 0.12));
  color: var(--text-h, #1d4ed8);
  text-decoration: none;
  font-weight: 600;
  font-size: 0.95rem;
  border: 1px solid var(--accent-border, rgba(37, 99, 235, 0.35));
  white-space: nowrap;
}
.btn-carrito:hover {
  background: rgba(37, 99, 235, 0.2);
}
</style>
