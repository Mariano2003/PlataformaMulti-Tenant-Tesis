<script setup lang="ts">
defineProps<{
  loading: boolean
  error: string | null
  sinProductos: boolean
}>()
</script>

<template>
  <p v-if="loading" class="estado">Cargando…</p>
  <p v-else-if="error" class="estado error">{{ error }}</p>
  <p v-else-if="sinProductos" class="estado">No hay productos activos en esta tienda.</p>
</template>

<style scoped>
.estado {
  margin: 1rem 0;
  color: var(--text, #4b5563);
}
.estado.error {
  color: #b91c1c;
}
</style>
