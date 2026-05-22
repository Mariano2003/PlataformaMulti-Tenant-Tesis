<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { apiUrl } from '../config/api'
import { useAuthStore } from '../stores/auth'
import type { CrearNegocioPayload, NegocioPublico } from '../types/api'

const auth = useAuthStore()

const tiendas = ref<NegocioPublico[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const crearSlug = ref('')
const crearNombre = ref('')
const crearDesc = ref('')
const crearEmail = ref('')
const crearDueñoEmail = ref('')
const crearDueñoPassword = ref('')
const crearDueñoNombre = ref('')
const crearDueñoApellido = ref('')
const creando = ref(false)
const crearMsg = ref<string | null>(null)
const crearError = ref<string | null>(null)

const rstEmail = ref('')
const rstPwd = ref('')
const rstLoading = ref(false)
const rstMsg = ref<string | null>(null)
const rstErr = ref<string | null>(null)

function mensajeErrorApi(bodyText: string, status: number): string {
  try {
    const j = JSON.parse(bodyText) as {
      error?: string
      title?: string
      errors?: Record<string, string[] | undefined>
    }
    if (typeof j.error === 'string' && j.error) return j.error
    if (j.errors && typeof j.errors === 'object') {
      const lineas = Object.entries(j.errors).flatMap(([k, v]) =>
        (v ?? []).map((msg) => `${k}: ${msg}`),
      )
      if (lineas.length) return lineas.join(' · ')
    }
    if (typeof j.title === 'string' && j.title) return j.title
  } catch {
    /* body no JSON */
  }
  if (status === 401)
    return 'Sesión vencida o sin permiso (401). Cerrá sesión y volvé a entrar como SuperAdmin.'
  return `Error ${status}`
}

async function cargarLista() {
  loading.value = true
  error.value = null
  try {
    const res = await fetch(apiUrl('/api/negocios'))
    if (!res.ok) {
      error.value = `No se pudo cargar el listado (${res.status}).`
      return
    }
    tiendas.value = (await res.json()) as NegocioPublico[]
  } catch {
    error.value = 'No se pudo conectar con la API.'
  } finally {
    loading.value = false
  }
}

onMounted(() => void cargarLista())

async function crearTienda() {
  crearMsg.value = null
  crearError.value = null
  if (!auth.token) {
    crearError.value = 'Sesión no válida.'
    return
  }

  const dueñoMail = crearDueñoEmail.value.trim()
  if (dueñoMail) {
    if (!crearDueñoNombre.value.trim()) {
      crearError.value = 'Si cargás el email del dueño, el nombre es obligatorio.'
      return
    }
    if (crearDueñoPassword.value.length < 8) {
      crearError.value = 'La contraseña del dueño debe tener al menos 8 caracteres.'
      return
    }
  }

  creando.value = true
  try {
    const body: CrearNegocioPayload = {
      slug: crearSlug.value.trim().toLowerCase(),
      nombre: crearNombre.value.trim(),
      descripcionCorta: crearDesc.value.trim() || undefined,
      emailContacto: crearEmail.value.trim() || undefined,
    }
    if (dueñoMail) {
      body.tiendaAdminEmail = dueñoMail
      body.tiendaAdminPassword = crearDueñoPassword.value
      body.tiendaAdminNombre = crearDueñoNombre.value.trim()
      const ap = crearDueñoApellido.value.trim()
      body.tiendaAdminApellido = ap || undefined
    }

    const res = await fetch(apiUrl('/api/negocios'), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${auth.token}`,
      },
      body: JSON.stringify(body),
    })
    const text = await res.text()
    if (!res.ok) {
      crearError.value = mensajeErrorApi(text, res.status)
      return
    }

    const creado = JSON.parse(text) as NegocioPublico
    let msg = 'Tienda creada correctamente.'
    if (creado.adminTiendaCreado && creado.adminTiendaEmail) {
      msg += ` Se creó el usuario del dueño (${creado.adminTiendaEmail}). Pasale ese mail y la contraseña que cargaste: puede entrar en «Acceso» o en /admin/${creado.slug}/login.`
    }
    crearMsg.value = msg

    crearSlug.value = ''
    crearNombre.value = ''
    crearDesc.value = ''
    crearEmail.value = ''
    crearDueñoEmail.value = ''
    crearDueñoPassword.value = ''
    crearDueñoNombre.value = ''
    crearDueñoApellido.value = ''
    await cargarLista()
  } catch {
    crearError.value = 'No se pudo conectar con la API.'
  } finally {
    creando.value = false
  }
}

async function restablecerClaveUsuario() {
  rstMsg.value = null
  rstErr.value = null
  if (!auth.token) {
    rstErr.value = 'Sesión no válida.'
    return
  }
  if (rstPwd.value.length < 8) {
    rstErr.value = 'La contraseña nueva debe tener al menos 8 caracteres.'
    return
  }
  rstLoading.value = true
  try {
    const res = await fetch(apiUrl('/api/auth/admin/restablecer-clave-usuario'), {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${auth.token}`,
      },
      body: JSON.stringify({
        email: rstEmail.value.trim().toLowerCase(),
        nuevaPassword: rstPwd.value,
      }),
    })
    const text = await res.text()
    if (!res.ok) {
      rstErr.value = mensajeErrorApi(text, res.status)
      return
    }
    rstMsg.value = 'Contraseña actualizada. Esa persona puede entrar con el mail indicado y la clave nueva.'
    rstPwd.value = ''
  } catch {
    rstErr.value = 'No se pudo conectar con la API.'
  } finally {
    rstLoading.value = false
  }
}
</script>

<template>
  <main class="sap">
    <header class="sap__head">
      <div>
        <h1>Panel plataforma</h1>
        <p class="sap__sub">
          Gestioná los negocios del SaaS. Solo vos como <strong>SuperAdmin</strong> ves esta pantalla y el
          alta de tiendas.
        </p>
      </div>
      <div class="sap__actions">
        <RouterLink class="sap__link" :to="{ name: 'tiendas' }">Ir a comprar (tiendas)</RouterLink>
        <button type="button" class="sap__out" @click="auth.cerrarSesion()">Salir</button>
      </div>
    </header>

    <section class="sap__panel sap__panel--accent">
      <h2 class="sap__h2">Alta de nueva tienda</h2>
      <p class="sap__hint">
        El <code>slug</code> es la URL de la tienda (minúsculas, números y guiones). Los
        <strong>clientes</strong> se registran solos en <code>/registro</code>; desde acá solo das de alta
        la tienda y el <strong>dueño</strong> (AdminTienda).
      </p>
      <form class="sap__form" @submit.prevent="crearTienda">
        <h3 class="sap__h3">Datos del negocio</h3>
        <label class="sap__field">
          <span>Slug</span>
          <input v-model="crearSlug" required placeholder="ej. mi-panaderia" autocomplete="off" />
        </label>
        <label class="sap__field">
          <span>Nombre</span>
          <input v-model="crearNombre" required maxlength="200" />
        </label>
        <label class="sap__field sap__field--grow">
          <span>Descripción corta (opcional)</span>
          <input v-model="crearDesc" maxlength="500" />
        </label>
        <label class="sap__field">
          <span>Email contacto público (opcional)</span>
          <input v-model="crearEmail" type="email" maxlength="200" />
        </label>

        <h3 class="sap__h3 sap__h3--sep">Dueño — panel de la tienda (recomendado)</h3>
        <p class="sap__hint sap__hint--tight">
          El dueño no puede registrarse en la web pública: cargá su email, nombre y contraseña inicial
          (mín. 8). Entrará en <code>/admin/&lt;slug&gt;/login</code>.
        </p>
        <label class="sap__field sap__field--grow">
          <span>Email del dueño (login)</span>
          <input
            v-model="crearDueñoEmail"
            type="email"
            maxlength="200"
            autocomplete="off"
            placeholder="dueño@ejemplo.com"
          />
        </label>
        <label class="sap__field">
          <span>Contraseña inicial</span>
          <input
            v-model="crearDueñoPassword"
            type="password"
            autocomplete="new-password"
            minlength="8"
            placeholder="Mín. 8 caracteres"
          />
        </label>
        <label class="sap__field">
          <span>Nombre</span>
          <input v-model="crearDueñoNombre" maxlength="100" autocomplete="off" />
        </label>
        <label class="sap__field">
          <span>Apellido (opcional)</span>
          <input v-model="crearDueñoApellido" maxlength="100" autocomplete="off" />
        </label>

        <button type="submit" class="sap__btn sap__btn--submit" :disabled="creando">
          {{ creando ? 'Creando…' : 'Crear tienda y dueño' }}
        </button>
      </form>
      <p v-if="crearMsg" class="sap__ok">{{ crearMsg }}</p>
      <p v-if="crearError" class="sap__err">{{ crearError }}</p>
    </section>

    <section class="sap__panel">
      <h2 class="sap__h2">Restablecer contraseña de un usuario</h2>
      <p class="sap__hint">
        Sin correo ni SMTP: definís una clave nueva para un dueño de tienda (u otro usuario existente).
        Los clientes suelen usar «Olvidé mi contraseña» o registrarse en <code>/registro</code>.
      </p>
      <form class="sap__form" @submit.prevent="restablecerClaveUsuario">
        <label class="sap__field sap__field--grow">
          <span>Email del usuario</span>
          <input v-model="rstEmail" type="email" required maxlength="200" autocomplete="off" />
        </label>
        <label class="sap__field">
          <span>Nueva contraseña</span>
          <input
            v-model="rstPwd"
            type="password"
            required
            minlength="8"
            maxlength="200"
            autocomplete="new-password"
          />
        </label>
        <button type="submit" class="sap__btn sap__btn--submit" :disabled="rstLoading">
          {{ rstLoading ? 'Guardando…' : 'Guardar contraseña' }}
        </button>
      </form>
      <p v-if="rstMsg" class="sap__ok">{{ rstMsg }}</p>
      <p v-if="rstErr" class="sap__err">{{ rstErr }}</p>
    </section>

    <section class="sap__panel">
      <h2 class="sap__h2">Tiendas registradas</h2>
      <p v-if="loading" class="sap__muted">Cargando…</p>
      <p v-else-if="error" class="sap__err">{{ error }}</p>
      <p v-else-if="!tiendas.length" class="sap__muted">No hay tiendas activas todavía.</p>
      <ul v-else class="sap__lista">
        <li v-for="n in tiendas" :key="n.id" class="sap__row">
          <div class="sap__row-main">
            <span class="sap__nombre">{{ n.nombre }}</span>
            <span class="sap__slug">{{ n.slug }}</span>
            <span class="sap__id">id: {{ n.id }}</span>
          </div>
          <div class="sap__row-links">
            <RouterLink :to="{ name: 'tienda', params: { slug: n.slug } }">Tienda pública</RouterLink>
            <RouterLink :to="{ name: 'admin-pedidos', params: { slug: n.slug } }">Panel admin</RouterLink>
          </div>
        </li>
      </ul>
    </section>
  </main>
</template>

<style scoped>
.sap {
  padding: 1.5rem 1.25rem 3rem;
  max-width: 52rem;
  margin: 0 auto;
  text-align: left;
}
.sap__head {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1.75rem;
}
.sap__head h1 {
  margin: 0 0 0.35rem;
  font-size: 1.65rem;
}
.sap__sub {
  margin: 0;
  max-width: 36rem;
  line-height: 1.5;
  color: var(--text, #4b5563);
  font-size: 0.95rem;
}
.sap__actions {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.5rem;
}
.sap__link {
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--accent, #2563eb);
  text-decoration: none;
}
.sap__link:hover {
  text-decoration: underline;
}
.sap__out {
  font-size: 0.85rem;
  padding: 0.35rem 0.65rem;
  border-radius: 8px;
  border: 1px solid var(--border, #d1d5db);
  background: transparent;
  cursor: pointer;
}
.sap__panel {
  margin-bottom: 1.5rem;
  padding: 1.25rem;
  border-radius: 12px;
  border: 1px solid var(--border, #e5e7eb);
  background: var(--surface, #fff);
}
.sap__panel--accent {
  border-color: rgba(37, 99, 235, 0.35);
  background: rgba(37, 99, 235, 0.04);
}
.sap__h2 {
  margin: 0 0 0.75rem;
  font-size: 1.1rem;
}
.sap__hint {
  margin: 0 0 1rem;
  font-size: 0.85rem;
  color: var(--text-muted, #6b7280);
  line-height: 1.45;
}
.sap__form {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(11rem, 1fr));
  gap: 0.85rem;
  align-items: end;
}
.sap__h3 {
  grid-column: 1 / -1;
  margin: 0;
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--text-h, #111827);
}
.sap__h3--sep {
  margin-top: 0.35rem;
}
.sap__hint--tight {
  grid-column: 1 / -1;
  margin-top: 0;
  margin-bottom: 0;
}
.sap__btn--submit {
  grid-column: 1 / -1;
  justify-self: start;
  margin-top: 0.25rem;
}
.sap__field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  font-size: 0.85rem;
}
.sap__field--grow {
  grid-column: 1 / -1;
}
@media (min-width: 640px) {
  .sap__field--grow {
    grid-column: span 2;
  }
}
.sap__field input {
  padding: 0.45rem 0.55rem;
  border-radius: 8px;
  border: 1px solid var(--border, #d1d5db);
}
.sap__btn {
  padding: 0.5rem 1rem;
  border-radius: 8px;
  border: none;
  background: var(--accent, #2563eb);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
  height: fit-content;
}
.sap__btn:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}
.sap__ok {
  margin: 0.75rem 0 0;
  font-size: 0.9rem;
  color: #15803d;
}
.sap__err {
  margin: 0.75rem 0 0;
  font-size: 0.9rem;
  color: #b91c1c;
}
.sap__muted {
  color: var(--text-muted, #6b7280);
  font-size: 0.95rem;
}
.sap__lista {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}
.sap__row {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.75rem;
  border-radius: 8px;
  border: 1px solid var(--border, #e5e7eb);
}
.sap__row-main {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}
.sap__nombre {
  font-weight: 600;
}
.sap__slug {
  font-family: ui-monospace, monospace;
  font-size: 0.85rem;
  color: var(--text, #374151);
}
.sap__id {
  font-size: 0.72rem;
  color: var(--text-muted, #9ca3af);
  word-break: break-all;
}
.sap__row-links {
  display: flex;
  flex-wrap: wrap;
  gap: 0.65rem;
  align-items: center;
}
.sap__row-links a {
  font-size: 0.88rem;
  color: var(--accent, #2563eb);
  text-decoration: none;
}
.sap__row-links a:hover {
  text-decoration: underline;
}
code {
  font-size: 0.8rem;
}
</style>
