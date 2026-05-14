<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useCarritoStore } from '../../stores/carrito'
import type { ProductoPublico } from '../../types/api'
import { usePrecioFmt } from '../../composables/usePrecioFmt'

const props = defineProps<{
  producto: ProductoPublico
}>()

const carrito = useCarritoStore()
const precioFmt = usePrecioFmt()
const cantidad = ref(1)
const imagenRota = ref(false)

const enCarrito = computed(() => carrito.cantidadEnCarrito(props.producto.id))
const stockDisponible = computed(() => carrito.stockDisponible(props.producto))
const sinStock = computed(
  () => props.producto.stock < 1 || stockDisponible.value < 1,
)

watch(
  () => props.producto.imagenUrl,
  () => {
    imagenRota.value = false
  },
)

watch(stockDisponible, (d) => {
  if (d < 1) cantidad.value = 1
  else if (cantidad.value > d) cantidad.value = d
})

watch(
  () => props.producto.stock,
  () => {
    const d = stockDisponible.value
    if (d < 1) cantidad.value = 1
    else if (cantidad.value > d) cantidad.value = d
  },
)

function agregar() {
  if (sinStock.value) return
  carrito.agregar(props.producto, cantidad.value)
  cantidad.value = 1
}
</script>

<template>
  <article class="card">
    <div v-if="producto.imagenUrl" class="thumb-wrap">
      <img
        v-show="!imagenRota"
        class="thumb"
        :src="producto.imagenUrl"
        :alt="`Foto de ${producto.nombre}`"
        loading="lazy"
        @error="imagenRota = true"
      />
      <p v-show="imagenRota" class="thumb-fail">Imagen no disponible</p>
    </div>
    <h2>{{ producto.nombre }}</h2>
    <p v-if="producto.descripcionCorta" class="card-desc">
      {{ producto.descripcionCorta }}
    </p>
    <p class="precio">{{ precioFmt(producto.precio) }}</p>
    <p v-if="sinStock" class="stock stock--agotado">Sin stock</p>
    <template v-else>
      <p class="stock">
        Disponible: <strong>{{ stockDisponible }}</strong>
        <span v-if="producto.stock > 0" class="stock-total">
          (total en tienda: {{ producto.stock
          }}<span v-if="enCarrito > 0"> · {{ enCarrito }} en tu carrito</span>)
        </span>
      </p>
    </template>
    <div class="acciones">
      <label class="qty">
        <span class="sr-only">Cantidad</span>
        <input
          v-model.number="cantidad"
          type="number"
          min="1"
          :max="Math.max(1, stockDisponible)"
          :disabled="sinStock"
        />
      </label>
      <button type="button" class="btn" :disabled="sinStock" @click="agregar">
        Agregar al carrito
      </button>
    </div>
  </article>
</template>

<style scoped>
.card {
  border: 1px solid var(--border, #e5e7eb);
  border-radius: 12px;
  padding: 1rem 1.1rem;
  background: var(--bg, #fff);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
}
.thumb-wrap {
  margin: -0.25rem -0.35rem 0.65rem;
  border-radius: 10px;
  overflow: hidden;
  background: var(--code-bg, #f3f4f6);
  aspect-ratio: 4 / 3;
  display: grid;
  place-items: center;
}
.thumb {
  grid-area: 1 / 1;
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.thumb-fail {
  grid-area: 1 / 1;
  margin: 0;
  padding: 0.75rem;
  font-size: 0.82rem;
  color: var(--text, #6b7280);
  text-align: center;
}
h2 {
  margin: 0 0 0.35rem;
  font-size: 1.05rem;
}
.card-desc {
  margin: 0 0 0.5rem;
  font-size: 0.9rem;
  line-height: 1.4;
  color: var(--text, #6b7280);
}
.precio {
  margin: 0;
  font-weight: 600;
  color: var(--text-h, #111827);
}
.stock {
  margin: 0.35rem 0 0.75rem;
  font-size: 0.85rem;
  color: var(--text, #6b7280);
}
.stock--agotado {
  font-weight: 600;
  color: #b91c1c;
}
.stock-total {
  display: block;
  margin-top: 0.2rem;
  font-size: 0.8rem;
  font-weight: normal;
  opacity: 0.9;
}
.acciones {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}
.qty input {
  width: 4rem;
  padding: 0.35rem 0.5rem;
  border: 1px solid var(--border, #d1d5db);
  border-radius: 6px;
  font-size: 1rem;
}
.btn {
  padding: 0.4rem 0.75rem;
  border-radius: 8px;
  border: 1px solid #2563eb;
  background: #2563eb;
  color: #fff;
  font-size: 0.9rem;
  cursor: pointer;
}
.btn:hover:not(:disabled) {
  background: #1d4ed8;
}
.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  border: 0;
}
</style>
