<script setup lang="ts">
import { RouterLink } from 'vue-router'

defineProps<{ slug: string }>()
</script>

<template>
  <nav class="admin-tabs" aria-label="Secciones del panel">
    <RouterLink
      class="admin-tabs-link"
      active-class="admin-tabs-link--active"
      :to="{ name: 'admin-pedidos', params: { slug } }"
    >
      Pedidos
    </RouterLink>
    <RouterLink
      class="admin-tabs-link"
      active-class="admin-tabs-link--active"
      :to="{ name: 'admin-productos', params: { slug } }"
    >
      Productos
    </RouterLink>
    <RouterLink
      class="admin-tabs-link"
      active-class="admin-tabs-link--active"
      :to="{ name: 'admin-categorias', params: { slug } }"
    >
      Categorías
    </RouterLink>
    <RouterLink
      class="admin-tabs-link"
      active-class="admin-tabs-link--active"
      :to="{ name: 'admin-analytics', params: { slug } }"
    >
      Analytics
    </RouterLink>
    <RouterLink
      class="admin-tabs-link"
      active-class="admin-tabs-link--active"
      :to="{ name: 'admin-chat', params: { slug } }"
    >
      Chat
    </RouterLink>
    <RouterLink
      class="admin-tabs-link"
      active-class="admin-tabs-link--active"
      :to="{ name: 'admin-mercadopago', params: { slug } }"
    >
      Pagos (MP)
    </RouterLink>
  </nav>
</template>
