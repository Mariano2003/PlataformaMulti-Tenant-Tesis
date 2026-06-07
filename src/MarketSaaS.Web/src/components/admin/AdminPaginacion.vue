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
    v-if="total > 0"
    class="admin-paginacion"
    aria-label="Paginación"
  >
    <p class="admin-paginacion__info">
      {{ total }} en total
      <template v-if="totalPaginas > 1">
        · página {{ pagina }} de {{ totalPaginas }}
      </template>
    </p>
    <div v-if="totalPaginas > 1" class="admin-paginacion__btns">
      <button
        type="button"
        class="btn-ghost"
        :disabled="cargando || pagina <= 1"
        @click="emit('anterior')"
      >
        Anterior
      </button>
      <button
        type="button"
        class="btn-ghost"
        :disabled="cargando || pagina >= totalPaginas"
        @click="emit('siguiente')"
      >
        Siguiente
      </button>
    </div>
  </nav>
</template>

<style scoped>
.admin-paginacion {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-top: 1rem;
  padding-top: 0.75rem;
  border-top: 1px solid var(--border, #e5e7eb);
}
.admin-paginacion__info {
  margin: 0;
  font-size: 0.88rem;
  color: var(--text-muted, #6b7280);
}
.admin-paginacion__btns {
  display: flex;
  gap: 0.5rem;
}
</style>
