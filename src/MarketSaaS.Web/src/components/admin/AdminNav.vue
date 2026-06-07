<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import { useAdminPedidosNovedades } from '../../composables/useAdminPedidosNovedades'

const props = defineProps<{ slug: string }>()

const slugRef = computed(() => props.slug)
const { novedades, hayNovedades } = useAdminPedidosNovedades(slugRef)
</script>

<template>
  <nav class="admin-tabs" aria-label="Secciones del panel">
    <RouterLink
      class="admin-tabs-link"
      active-class="admin-tabs-link--active"
      :to="{ name: 'admin-pedidos', params: { slug } }"
    >
      Pedidos
      <span v-if="hayNovedades" class="admin-badge" :title="`${novedades} pedido(s) pagado(s) sin revisar`">
        {{ novedades > 99 ? '99+' : novedades }}
      </span>
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

<style scoped>
.admin-tabs-link {
  position: relative;
}
.admin-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.25rem;
  height: 1.25rem;
  margin-left: 0.35rem;
  padding: 0 0.35rem;
  border-radius: 999px;
  background: #dc2626;
  color: #fff;
  font-size: 0.7rem;
  font-weight: 700;
  line-height: 1;
  vertical-align: middle;
}
</style>
