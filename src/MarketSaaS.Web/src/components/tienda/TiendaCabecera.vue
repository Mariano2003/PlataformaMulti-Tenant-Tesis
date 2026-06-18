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
  <header class="tienda-hero">
    <div class="titulos">
      <h1>{{ negocio?.nombre ?? 'Tienda' }}</h1>
      <p class="meta">Tienda: <code>{{ slug }}</code></p>
    </div>
    <RouterLink
      v-if="slug && totalCarrito > 0"
      class="btn-carrito-hero"
      :to="{ name: 'checkout', params: { slug } }"
    >
      Carrito ({{ totalCarrito }})
    </RouterLink>
  </header>
</template>

<style scoped>
.titulos {
  flex: 1;
  min-width: 12rem;
}
.meta code {
  font-size: 0.9em;
  background: rgba(255, 255, 255, 0.2);
  color: #fff;
}
</style>
