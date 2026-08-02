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
  mercadoPagoConectadoOAuth?: boolean
  mercadoPagoOAuthDisponible?: boolean
  mercadoPagoUserId?: string | null
  mercadoPagoConectadoEn?: string | null
}

const contexto = ref<ContextoAdmin | null>(null)
const cargando = ref(true)
const conectando = ref(false)
const guardandoToken = ref(false)
const accessTokenManual = ref('')
const error = ref<string | null>(null)
const okMsg = ref<string | null>(null)

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

function procesarRetornoOAuth() {
  const oauth = route.query.mp_oauth
  if (oauth === 'ok') {
    okMsg.value = 'Cuenta de Mercado Pago vinculada correctamente.'
    void router.replace({
      name: 'admin-mercadopago',
      params: { slug: slug.value },
    })
  } else if (oauth === 'error') {
    const msg = typeof route.query.mp_msg === 'string' ? route.query.mp_msg : 'No se pudo vincular la cuenta.'
    error.value = msg
    void router.replace({
      name: 'admin-mercadopago',
      params: { slug: slug.value },
    })
  }
}

async function conectarMercadoPago() {
  okMsg.value = null
  error.value = null
  const s = slug.value
  if (!s) return
  conectando.value = true
  try {
    const res = await authedFetch(
      `/api/negocios/${encodeURIComponent(s)}/admin/mercadopago/oauth/iniciar`,
      { method: 'POST' },
    )
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
    const data = (await res.json()) as { authorizationUrl: string }
    if (!data.authorizationUrl) {
      error.value = 'La API no devolvió la URL de autorización.'
      return
    }
    window.location.href = data.authorizationUrl
  } catch {
    error.value = 'Error de red.'
  } finally {
    conectando.value = false
  }
}

onMounted(() => {
  procesarRetornoOAuth()
  void cargarContexto()
})

const desconectando = ref(false)

async function guardarTokenManual() {
  okMsg.value = null
  error.value = null
  const s = slug.value
  const token = accessTokenManual.value.trim()
  if (!s) return
  if (!token) {
    error.value = 'Pegá el Access Token de prueba (TEST-…) de Mercado Pago Developers.'
    return
  }
  guardandoToken.value = true
  try {
    const res = await authedFetch(`/api/negocios/${encodeURIComponent(s)}/admin/mercadopago`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ accessToken: token }),
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
    accessTokenManual.value = ''
    okMsg.value = 'Access Token guardado. Ya podés cobrar con Mercado Pago (sandbox).'
    await cargarContexto()
  } catch {
    error.value = 'Error de red.'
  } finally {
    guardandoToken.value = false
  }
}

async function desconectarMercadoPago() {
  if (!confirm('¿Desvincular la cuenta de Mercado Pago? Dejarás de cobrar hasta reconectar.')) return
  okMsg.value = null
  error.value = null
  const s = slug.value
  if (!s) return
  desconectando.value = true
  try {
    const res = await authedFetch(
      `/api/negocios/${encodeURIComponent(s)}/admin/mercadopago/oauth/desconectar`,
      { method: 'POST' },
    )
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
    okMsg.value = 'Cuenta desvinculada.'
    await cargarContexto()
  } catch {
    error.value = 'Error de red.'
  } finally {
    desconectando.value = false
  }
}

function salir() {
  auth.cerrarSesion()
}
</script>

<template>
  <div class="admin-page">
    <header class="admin-head">
      <div>
        <h1>Mercado Pago — {{ contexto?.nombre ?? '…' }}</h1>
        <p class="admin-sub">
          Vinculá tu cuenta de Mercado Pago para cobrar las ventas de esta tienda. Los clientes pagan con
          Checkout Pro y el dinero ingresa a la cuenta que autorices.
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
          <strong v-if="contexto.mercadoPagoConectadoOAuth">Cuenta vinculada</strong>
          <strong v-else-if="contexto.mercadoPagoTiendaConfigurado">Cuenta configurada</strong>
          <strong v-else>Sin cuenta vinculada</strong>
        </p>

        <template v-if="contexto.mercadoPagoOAuthDisponible">
          <p class="hint">
            Autorizá la app de la plataforma con un clic. Los cobros de tu tienda irán a tu Mercado Pago sin
            copiar tokens ni configurar webhooks manualmente.
          </p>
          <p v-if="contexto.mercadoPagoConectadoOAuth" class="oauth-meta">
            Vinculada vía OAuth
            <span v-if="contexto.mercadoPagoUserId"> (usuario {{ contexto.mercadoPagoUserId }})</span>
            <span v-if="contexto.mercadoPagoConectadoEn">
              — {{ new Date(contexto.mercadoPagoConectadoEn).toLocaleString() }}
            </span>
          </p>
          <button
            type="button"
            class="btn-connect"
            :disabled="conectando"
            @click="conectarMercadoPago"
          >
            {{
              conectando
                ? 'Redirigiendo a Mercado Pago…'
                : contexto.mercadoPagoConectadoOAuth
                  ? 'Reconectar cuenta'
                  : 'Conectar con Mercado Pago'
            }}
          </button>
          <button
            v-if="contexto.mercadoPagoConectadoOAuth || contexto.mercadoPagoTiendaConfigurado"
            type="button"
            class="btn-disconnect"
            :disabled="desconectando || conectando"
            @click="desconectarMercadoPago"
          >
            {{ desconectando ? 'Desvinculando…' : 'Desvincular cuenta' }}
          </button>
        </template>
        <p v-else class="hint">
          La vinculación OAuth no está habilitada en la plataforma. Podés pegar un Access Token de
          prueba abajo, o pedirle al administrador que configure OAuth en la API.
        </p>

        <hr class="sep" />

        <h2 class="subtitulo">Atajo para pruebas (recomendado)</h2>
        <p class="hint">
          En
          <a href="https://www.mercadopago.com.ar/developers/panel/app" target="_blank" rel="noopener"
            >Mercado Pago Developers</a
          >
          → tu app → <strong>Credenciales de prueba</strong> → copiá el Access Token (<code>TEST-…</code>)
          y pegalo acá. No hace falta OAuth ni vendedor de prueba.
        </p>
        <label class="field">
          <span>Access Token de prueba</span>
          <input
            v-model="accessTokenManual"
            type="password"
            autocomplete="off"
            placeholder="TEST-…"
            maxlength="512"
          />
        </label>
        <button
          type="button"
          class="btn-connect btn-connect--alt"
          :disabled="guardandoToken || conectando"
          @click="guardarTokenManual"
        >
          {{ guardandoToken ? 'Guardando…' : 'Guardar token de prueba' }}
        </button>

        <p v-if="error" class="err">{{ error }}</p>
        <p v-if="okMsg" class="ok">{{ okMsg }}</p>
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
.err {
  color: #b91c1c;
  font-size: 0.9rem;
}
.ok {
  color: #15803d;
  font-size: 0.9rem;
}
.btn-connect {
  display: block;
  width: 100%;
  margin-bottom: 0.5rem;
  padding: 0.65rem 1rem;
  border-radius: 8px;
  border: none;
  background: #009ee3;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}
.btn-connect:disabled {
  opacity: 0.65;
}
.btn-connect--alt {
  background: #0f766e;
}
.sep {
  border: none;
  border-top: 1px solid var(--border, #e5e7eb);
  margin: 1.25rem 0;
}
.subtitulo {
  margin: 0 0 0.5rem;
  font-size: 1rem;
  font-weight: 600;
}
.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin-bottom: 0.75rem;
  font-size: 0.9rem;
}
.field input {
  padding: 0.55rem 0.65rem;
  border-radius: 8px;
  border: 1px solid var(--border, #e5e7eb);
  font-family: ui-monospace, monospace;
  font-size: 0.85rem;
}
.btn-disconnect {
  display: block;
  width: 100%;
  margin-bottom: 0.5rem;
  padding: 0.55rem 1rem;
  border-radius: 8px;
  border: 1px solid #fecaca;
  background: #fff;
  color: #b91c1c;
  font-weight: 600;
  cursor: pointer;
}
.btn-disconnect:disabled {
  opacity: 0.65;
}
.oauth-meta {
  margin: 0 0 0.75rem;
  font-size: 0.85rem;
  color: #15803d;
}
</style>
