<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AdminNav from '../../components/admin/AdminNav.vue'
import { useAuthedFetch } from '../../composables/useAuthedFetch'
import { useAuthStore } from '../../stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const authedFetch = useAuthedFetch()

const slug = computed(() => (route.params.slug as string) || '')

interface ContextoAdmin {
  negocioId: string
  slug: string
  nombre: string
  activo: boolean
  mercadoPagoTiendaConfigurado?: boolean
}

const contexto = ref<ContextoAdmin | null>(null)
const cargando = ref(true)
const guardando = ref(false)
const error = ref<string | null>(null)
const okMsg = ref<string | null>(null)

const accessToken = ref('')
const webhookSecret = ref('')
const quitarTokenTienda = ref(false)
const quitarSecretTienda = ref(false)

async function cargarContexto() {
  cargando.value = true
  error.value = null
  const s = slug.value
  if (!s) {
    error.value = 'Slug inválido.'
    cargando.value = false
    return
  }
  try {
    const res = await authedFetch(`/api/negocios/${encodeURIComponent(s)}/admin/contexto`)
    if (res.status === 401) {
      auth.cerrarSesion()
      await router.replace({
        name: 'admin-login',
        params: { slug: s },
        query: { redirect: route.fullPath },
      })
      return
    }
    if (!res.ok) {
      error.value = res.status === 403 ? 'Sin permiso para esta tienda.' : `Error ${res.status}`
      return
    }
    contexto.value = (await res.json()) as ContextoAdmin
  } catch {
    error.value = 'Error de red.'
  } finally {
    cargando.value = false
  }
}

onMounted(() => void cargarContexto())

async function guardar() {
  okMsg.value = null
  error.value = null
  const s = slug.value
  if (!s) return

  const body: Record<string, string> = {}
  if (quitarTokenTienda.value) body.accessToken = ''
  else if (accessToken.value.trim()) body.accessToken = accessToken.value.trim()

  if (quitarSecretTienda.value) body.webhookSecret = ''
  else if (webhookSecret.value.trim()) body.webhookSecret = webhookSecret.value.trim()

  if (Object.keys(body).length === 0) {
    error.value =
      'Indicá un token nuevo, el secreto del webhook, o marcá quitar token/secreto de la tienda.'
    return
  }

  guardando.value = true
  try {
    const res = await authedFetch(`/api/negocios/${encodeURIComponent(s)}/admin/mercadopago`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    if (res.status === 401) {
      auth.cerrarSesion()
      await router.replace({
        name: 'admin-login',
        params: { slug: s },
        query: { redirect: route.fullPath },
      })
      return
    }
    if (!res.ok) {
      try {
        const j = (await res.json()) as { error?: string }
        error.value = j.error ?? `Error ${res.status}`
      } catch {
        error.value = `Error ${res.status}`
      }
      return
    }
    okMsg.value = 'Listo. Los pagos de clientes usarán esta cuenta de Mercado Pago (si cargaste token).'
    accessToken.value = ''
    webhookSecret.value = ''
    quitarTokenTienda.value = false
    quitarSecretTienda.value = false
    await cargarContexto()
  } catch {
    error.value = 'Error de red.'
  } finally {
    guardando.value = false
  }
}

function salir() {
  auth.cerrarSesion()
  void router.push({ name: 'admin-login', params: { slug: slug.value } })
}
</script>

<template>
  <div class="admin-page">
    <header class="admin-head">
      <div>
        <h1>Mercado Pago — {{ contexto?.nombre ?? '…' }}</h1>
        <p class="admin-sub">
          Configurá la cuenta donde querés cobrar las ventas de esta tienda. Los clientes pagan con Checkout
          Pro; el dinero ingresa a la cuenta asociada al <strong>Access Token</strong> que pegues (tu app en
          Mercado Pago).
        </p>
      </div>
      <div class="admin-actions">
        <RouterLink class="btn-ghost" :to="{ name: 'tienda', params: { slug } }">Ver tienda pública</RouterLink>
        <button type="button" class="btn-ghost" @click="salir">Salir</button>
      </div>
    </header>

    <AdminNav :slug="slug" />

    <p v-if="cargando" class="admin-sub">Cargando…</p>
    <p v-else-if="error && !contexto" class="admin-sub" style="color: #b91c1c">{{ error }}</p>

    <template v-else-if="contexto">
      <section class="admin-panel">
        <p class="estado">
          Estado:
          <strong v-if="contexto.mercadoPagoTiendaConfigurado">Token propio de la tienda cargado</strong>
          <strong v-else>Sin token propio — se usa el de la plataforma (si existe en la API)</strong>
        </p>

        <p class="hint">
          En
          <a href="https://www.mercadopago.com.ar/developers/panel/app" target="_blank" rel="noopener"
            >Tus integraciones</a
          >
          creá una aplicación, copiá el Access Token (test o producción) y pegalo abajo. La URL de notificación
          se arma sola con el id de tu negocio para que el webhook pueda confirmar pagos.
        </p>

        <label class="field">
          <span>Access Token (no se muestra después de guardar)</span>
          <input
            v-model="accessToken"
            type="password"
            autocomplete="off"
            placeholder="APP_USR-…"
            :disabled="quitarTokenTienda"
          />
        </label>
        <label class="check">
          <input v-model="quitarTokenTienda" type="checkbox" />
          Quitar token de esta tienda y usar solo el global de la API
        </label>

        <label class="field">
          <span>Secreto del webhook (opcional, si validás firma en MP por esta app)</span>
          <input v-model="webhookSecret" type="password" autocomplete="off" :disabled="quitarSecretTienda" />
        </label>
        <label class="check">
          <input v-model="quitarSecretTienda" type="checkbox" />
          Quitar secreto por tienda (se usa el global si está configurado)
        </label>

        <p v-if="error" class="err">{{ error }}</p>
        <p v-if="okMsg" class="ok">{{ okMsg }}</p>

        <button type="button" class="btn-submit" :disabled="guardando" @click="guardar">
          {{ guardando ? 'Guardando…' : 'Guardar' }}
        </button>
      </section>
    </template>
  </div>
</template>

<style scoped>
.admin-panel {
  max-width: 36rem;
  margin-top: 1rem;
  padding: 1.25rem;
  border-radius: 12px;
  border: 1px solid var(--border, #e5e7eb);
  background: var(--surface, #fff);
}
.estado {
  margin: 0 0 1rem;
  font-size: 0.95rem;
}
.hint {
  margin: 0 0 1rem;
  font-size: 0.85rem;
  color: var(--text-muted, #6b7280);
  line-height: 1.45;
}
.hint a {
  color: var(--accent, #2563eb);
}
.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin-bottom: 1rem;
  font-size: 0.9rem;
}
.field input {
  padding: 0.5rem 0.65rem;
  border-radius: 8px;
  border: 1px solid var(--border, #d1d5db);
  font-family: ui-monospace, monospace;
  font-size: 0.85rem;
}
.check {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  margin: -0.5rem 0 1rem;
  font-size: 0.85rem;
  cursor: pointer;
}
.err {
  color: #b91c1c;
  font-size: 0.9rem;
}
.ok {
  color: #15803d;
  font-size: 0.9rem;
}
.btn-submit {
  margin-top: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 8px;
  border: none;
  background: var(--accent, #2563eb);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}
.btn-submit:disabled {
  opacity: 0.65;
}
</style>
