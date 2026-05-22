<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { apiUrl } from '../config/api'
import { useCarritoStore } from '../stores/carrito'
import { usePrecioFmt } from '../composables/usePrecioFmt'
import type { PedidoCreadoDto, PreferenciaMercadoPagoDto } from '../types/api'

const route = useRoute()
const carrito = useCarritoStore()
const precioFmt = usePrecioFmt()

const slug = computed(() => (route.params.slug as string) || '')

const clienteNombre = ref('')
const clienteEmail = ref('')
const clienteTelefono = ref('')
const enviando = ref(false)
const mensaje = ref<string | null>(null)
const mensajeOk = ref(false)

const slugOk = computed(
  () =>
    slug.value &&
    carrito.slugTienda === slug.value &&
    carrito.items.length > 0,
)

watch(slug, (s) => {
  if (s.trim()) carrito.setTienda(s)
})

async function leerError(res: Response): Promise<string> {
  try {
    const j = (await res.json()) as { error?: string }
    if (j?.error) return j.error
  } catch {
    /* vacío */
  }
  return `Error ${res.status}`
}

async function confirmarPedido() {
  mensaje.value = null
  mensajeOk.value = false
  if (!slugOk.value) return

  enviando.value = true
  try {
    const body = {
      lineas: carrito.items.map((l) => ({
        productoId: l.productoId,
        cantidad: l.cantidad,
      })),
      clienteNombre: clienteNombre.value.trim() || null,
      clienteEmail: clienteEmail.value.trim(),
      clienteTelefono: clienteTelefono.value.trim() || null,
    }

    const rPedido = await fetch(
      apiUrl(`/api/negocios/${encodeURIComponent(slug.value)}/pedidos`),
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      },
    )

    if (!rPedido.ok) {
      mensaje.value = await leerError(rPedido)
      return
    }

    const pedido = (await rPedido.json()) as PedidoCreadoDto

    const rPref = await fetch(
      apiUrl(
        `/api/negocios/${encodeURIComponent(slug.value)}/pedidos/${encodeURIComponent(pedido.id)}/mercadopago/preferencia`,
      ),
      { method: 'POST' },
    )

    if (!rPref.ok) {
      const detalle = await leerError(rPref)
      mensaje.value = `Pedido creado (${pedido.id}, estado ${pedido.estado}). No se pudo iniciar el pago: ${detalle}`
      mensajeOk.value = true
      carrito.vaciar()
      return
    }

    const pref = (await rPref.json()) as PreferenciaMercadoPagoDto
    carrito.vaciar()
    window.location.href = pref.urlPago
  } catch {
    mensaje.value =
      'No se pudo conectar con la API. ¿Está el backend en el puerto 5037?'
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="checkout-page">
    <nav class="breadcrumb">
      <RouterLink :to="{ name: 'tienda', params: { slug } }">← Volver a la tienda</RouterLink>
    </nav>

    <h1>Checkout</h1>

    <p v-if="!slug" class="checkout-alert">Slug inválido.</p>

    <template v-else-if="!slugOk">
      <p v-if="carrito.items.length === 0" class="checkout-alert">
        Tu carrito está vacío. Agregá productos desde
        <RouterLink :to="{ name: 'tienda', params: { slug } }">la tienda</RouterLink>.
      </p>
      <p v-else class="checkout-alert">
        El carrito pertenece a la tienda «{{ carrito.slugTienda }}». Abrí el checkout desde esa tienda o vaciá el carrito
        navegando a la tienda correcta.
      </p>
    </template>

    <template v-else>
      <section class="checkout-card">
        <h2>Resumen</h2>
        <ul class="checkout-lineas">
          <li v-for="l in carrito.items" :key="l.productoId" class="checkout-linea">
            <img
              v-if="l.imagenUrl"
              class="checkout-linea-thumb"
              :src="l.imagenUrl"
              :alt="`Foto de ${l.nombre}`"
              loading="lazy"
            />
            <div class="info">
              <span class="nombre">{{ l.nombre }}</span>
              <span class="sub">{{ precioFmt(l.precioUnitario) }} × {{ l.cantidad }}</span>
            </div>
            <div class="ctrl">
              <input
                type="number"
                min="1"
                :max="l.stockMax"
                :value="l.cantidad"
                class="qty"
                @change="
                  carrito.setCantidad(
                    l.productoId,
                    Number(($event.target as HTMLInputElement).value),
                  )
                "
              />
              <button type="button" class="link" @click="carrito.remover(l.productoId)">
                Quitar
              </button>
            </div>
          </li>
        </ul>
        <p class="total">
          Subtotal: <strong>{{ precioFmt(carrito.subtotal) }}</strong>
        </p>
      </section>

      <section class="checkout-card">
        <h2>Datos del cliente</h2>
        <label class="checkout-field">
          <span>Nombre</span>
          <input v-model="clienteNombre" type="text" maxlength="120" autocomplete="name" />
        </label>
        <label class="checkout-field">
          <span>Email <em class="req">*</em></span>
          <input
            v-model="clienteEmail"
            type="email"
            required
            maxlength="200"
            autocomplete="email"
          />
        </label>
        <label class="checkout-field">
          <span>Teléfono</span>
          <input v-model="clienteTelefono" type="tel" maxlength="40" autocomplete="tel" />
        </label>
      </section>

      <p v-if="mensaje" class="checkout-alert" :class="{ 'checkout-alert--ok': mensajeOk }">
        {{ mensaje }}
      </p>

      <button
        type="button"
        class="btn-primary"
        :disabled="enviando || !clienteEmail.trim()"
        @click="confirmarPedido"
      >
        {{ enviando ? 'Enviando…' : 'Confirmar y pagar con Mercado Pago' }}
      </button>
      <p class="checkout-hint">
        Si Mercado Pago no está configurado en la API, el pedido igual puede crearse y verás un mensaje con el id.
      </p>
    </template>
  </div>
</template>

<style scoped>
.info {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  flex: 1 1 8rem;
  min-width: 0;
}
.nombre {
  font-weight: 600;
  color: var(--text-h);
}
.sub {
  font-size: 0.85rem;
  color: var(--text-muted);
}
.ctrl {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.qty {
  width: 3.5rem;
  padding: 0.25rem 0.4rem;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border-strong);
}
.link {
  border: none;
  background: none;
  color: #b91c1c;
  cursor: pointer;
  font-size: 0.9rem;
  text-decoration: underline;
}
.total {
  margin: 0.75rem 0 0;
  font-size: 1.05rem;
}
.total strong {
  color: var(--primary-dark);
}
.req {
  color: #b91c1c;
}
</style>
