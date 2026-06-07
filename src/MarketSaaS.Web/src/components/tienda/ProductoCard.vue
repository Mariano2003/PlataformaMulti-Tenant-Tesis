<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useCarritoStore } from '../../stores/carrito'
import type { ProductoPublico } from '../../types/api'
import { usePrecioFmt } from '../../composables/usePrecioFmt'
import { resolveImagenUrl } from '../../utils/resolveImagenUrl'

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

const imagenSrc = computed(() => resolveImagenUrl(props.producto.imagenUrl))

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
  <article class="producto-card">
    <div class="thumb-wrap">
      <img
        v-if="imagenSrc && !imagenRota"
        class="thumb"
        :src="imagenSrc"
        :alt="`Foto de ${producto.nombre}`"
        loading="lazy"
        @error="imagenRota = true"
      />
      <p v-else-if="imagenSrc && imagenRota" class="thumb-fail">Imagen no disponible</p>
      <p v-else class="thumb-placeholder" aria-hidden="true">Sin imagen</p>
    </div>
    <div class="producto-card__body">
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
            (total: {{ producto.stock
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
        <button type="button" class="btn-add" :disabled="sinStock" @click="agregar">
          Agregar al carrito
        </button>
      </div>
    </div>
  </article>
</template>

<style scoped>
.thumb-wrap {
  aspect-ratio: 4 / 3;
  background: var(--code-bg);
  display: grid;
  place-items: center;
  overflow: hidden;
}
.thumb {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.thumb-fail {
  margin: 0;
  padding: 0.75rem;
  font-size: 0.82rem;
  color: var(--text-muted);
  text-align: center;
}
.thumb-placeholder {
  margin: 0;
  font-size: 0.82rem;
  color: var(--text-muted);
  text-align: center;
}
.producto-card__body h2 {
  margin: 0 0 0.35rem;
  font-size: 1.08rem;
}
.card-desc {
  margin: 0 0 0.5rem;
  font-size: 0.9rem;
  line-height: 1.4;
  color: var(--text);
}
.stock {
  margin: 0.35rem 0 0.75rem;
  font-size: 0.85rem;
  color: var(--text-muted);
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
}
.acciones {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
  margin-top: auto;
}
.qty input {
  width: 4rem;
  padding: 0.4rem 0.5rem;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  font-size: 1rem;
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
