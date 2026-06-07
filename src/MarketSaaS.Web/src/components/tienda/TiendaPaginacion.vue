<script setup lang="ts">
const props = defineProps<{
  pagina: number
  totalPaginas: number
  total: number
  cargando?: boolean
}>()

const emit = defineEmits<{
  anterior: []
  siguiente: []
}>()
</script>

<template>
  <nav
    v-if="totalPaginas > 1"
    class="tienda-paginacion"
    aria-label="Paginación de productos"
  >
    <p class="tienda-paginacion__info">
      {{ total }} productos · página {{ pagina }} de {{ totalPaginas }}
    </p>
    <div class="tienda-paginacion__btns">
      <button
        type="button"
        class="tienda-paginacion__btn"
        :disabled="cargando || pagina <= 1"
        @click="emit('anterior')"
      >
        Anterior
      </button>
      <button
        type="button"
        class="tienda-paginacion__btn"
        :disabled="cargando || pagina >= totalPaginas"
        @click="emit('siguiente')"
      >
        Siguiente
      </button>
    </div>
  </nav>
</template>

<style scoped>
.tienda-paginacion {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin: 1.5rem 0 0;
  padding-top: 1rem;
  border-top: 1px solid var(--border, #e5e7eb);
}
.tienda-paginacion__info {
  margin: 0;
  font-size: 0.88rem;
  color: var(--text-muted, #6b7280);
}
.tienda-paginacion__btns {
  display: flex;
  gap: 0.5rem;
}
.tienda-paginacion__btn {
  padding: 0.45rem 0.85rem;
  border: 1px solid var(--border-strong, #d1d5db);
  border-radius: 8px;
  background: var(--surface, #fff);
  cursor: pointer;
  font-size: 0.9rem;
}
.tienda-paginacion__btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
