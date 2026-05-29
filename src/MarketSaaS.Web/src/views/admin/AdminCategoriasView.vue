<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AdminNav from '../../components/admin/AdminNav.vue'
import { useAuthedFetch } from '../../composables/useAuthedFetch'
import { useAuthStore } from '../../stores/auth'
import type { CategoriaAdminDto } from '../../types/api'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const authedFetch = useAuthedFetch()

const slug = computed(() => (route.params.slug as string) || '')

const categorias = ref<CategoriaAdminDto[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)
const msg = ref<string | null>(null)
const guardando = ref(false)
const operandoId = ref<string | null>(null)

const categoriaEdicion = ref<CategoriaAdminDto | null>(null)

const nuevo = reactive({
  nombre: '',
  orden: '' as string,
})

const edicion = reactive({
  nombre: '',
  orden: '' as string,
  activo: true,
})

function baseUrl() {
  return `/api/negocios/${encodeURIComponent(slug.value)}/admin/categorias`
}

async function leerErrorApi(res: Response): Promise<string> {
  try {
    const data = (await res.json()) as { error?: string }
    if (data?.error) return data.error
  } catch {
    /* ignore */
  }
  return `Error ${res.status}`
}

async function manejar401() {
  auth.cerrarSesion()
  await router.replace({
    name: 'admin-login',
    params: { slug: slug.value },
    query: { redirect: route.fullPath },
  })
}

async function cargar() {
  cargando.value = true
  error.value = null
  msg.value = null
  categorias.value = []
  const s = slug.value
  if (!s) {
    error.value = 'Slug inválido.'
    cargando.value = false
    return
  }
  try {
    const res = await authedFetch(baseUrl())
    if (res.status === 401) {
      await manejar401()
      return
    }
    if (res.status === 403) {
      error.value = 'No tenés permiso para este negocio.'
      return
    }
    if (!res.ok) {
      error.value = await leerErrorApi(res)
      return
    }
    categorias.value = (await res.json()) as CategoriaAdminDto[]
    if (categoriaEdicion.value) {
      const actual = categorias.value.find((c) => c.id === categoriaEdicion.value!.id)
      categoriaEdicion.value = actual ?? null
      if (actual) sincronizarEdicion(actual)
    }
  } catch {
    error.value = 'Error de red al cargar categorías.'
  } finally {
    cargando.value = false
  }
}

function sincronizarEdicion(c: CategoriaAdminDto) {
  edicion.nombre = c.nombre
  edicion.orden = String(c.orden)
  edicion.activo = c.activo
}

function abrirEdicion(c: CategoriaAdminDto) {
  msg.value = null
  categoriaEdicion.value = c
  sincronizarEdicion(c)
}

function cerrarEdicion() {
  categoriaEdicion.value = null
}

async function crear() {
  msg.value = null
  const nombre = nuevo.nombre.trim()
  if (!nombre) {
    msg.value = 'El nombre es obligatorio.'
    return
  }
  guardando.value = true
  const orden = nuevo.orden.trim() === '' ? null : Number(nuevo.orden)
  if (orden !== null && (Number.isNaN(orden) || orden < 0)) {
    msg.value = 'El orden debe ser un número ≥ 0.'
    guardando.value = false
    return
  }
  try {
    const body: { nombre: string; orden?: number } = { nombre }
    if (orden !== null) body.orden = orden
    const res = await authedFetch(baseUrl(), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    if (res.status === 401) {
      await manejar401()
      return
    }
    if (!res.ok) {
      msg.value = await leerErrorApi(res)
      return
    }
    nuevo.nombre = ''
    nuevo.orden = ''
    msg.value = 'Categoría creada.'
    await cargar()
  } catch {
    msg.value = 'Error de red al crear la categoría.'
  } finally {
    guardando.value = false
  }
}

async function guardarEdicion() {
  const c = categoriaEdicion.value
  if (!c) return
  msg.value = null
  const nombre = edicion.nombre.trim()
  if (!nombre) {
    msg.value = 'El nombre es obligatorio.'
    return
  }
  const orden = Number(edicion.orden.replace(',', '.'))
  if (Number.isNaN(orden) || orden < 0) {
    msg.value = 'El orden debe ser un número ≥ 0.'
    return
  }
  guardando.value = true
  try {
    const res = await authedFetch(`${baseUrl()}/${encodeURIComponent(c.id)}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        nombre,
        orden,
        activo: edicion.activo,
      }),
    })
    if (res.status === 401) {
      await manejar401()
      return
    }
    if (!res.ok) {
      msg.value = await leerErrorApi(res)
      return
    }
    msg.value = 'Cambios guardados.'
    cerrarEdicion()
    await cargar()
  } catch {
    msg.value = 'Error de red al guardar.'
  } finally {
    guardando.value = false
  }
}

async function eliminar(c: CategoriaAdminDto) {
  if (!confirm(`¿Eliminar la categoría «${c.nombre}»?`)) return
  msg.value = null
  operandoId.value = c.id
  try {
    const res = await authedFetch(`${baseUrl()}/${encodeURIComponent(c.id)}`, {
      method: 'DELETE',
    })
    if (res.status === 401) {
      await manejar401()
      return
    }
    if (!res.ok) {
      msg.value = await leerErrorApi(res)
      return
    }
    if (categoriaEdicion.value?.id === c.id) cerrarEdicion()
    msg.value = 'Categoría eliminada.'
    await cargar()
  } catch {
    msg.value = 'Error de red al eliminar.'
  } finally {
    operandoId.value = null
  }
}

function salir() {
  auth.cerrarSesion()
}

const msgEsError = computed(() => {
  const m = msg.value
  if (!m) return false
  return !['Categoría creada.', 'Cambios guardados.', 'Categoría eliminada.'].includes(m)
})

onMounted(() => {
  void cargar()
})
</script>

<template>
  <div class="admin-page">
    <header class="admin-head">
      <div>
        <h1>Categorías</h1>
        <p class="admin-sub">
          Tienda <code>{{ slug }}</code>
          <span v-if="auth.usuario"> · {{ auth.usuario.email }} ({{ auth.usuario.rol }})</span>
        </p>
      </div>
      <div class="admin-actions">
        <button type="button" class="btn-ghost" @click="cargar">Actualizar</button>
        <button type="button" class="btn-ghost" @click="salir">Salir</button>
      </div>
    </header>

    <AdminNav :slug="slug" />

    <nav class="admin-breadcrumb">
      <RouterLink to="/">Inicio</RouterLink>
      <span class="sep">·</span>
      <RouterLink :to="{ name: 'admin-productos', params: { slug } }">Productos</RouterLink>
      <span class="sep">·</span>
      <RouterLink :to="{ name: 'tienda', params: { slug } }">Ver tienda pública</RouterLink>
    </nav>

    <p v-if="cargando" class="admin-msg">Cargando…</p>
    <p v-else-if="error" class="admin-msg admin-msg--error">{{ error }}</p>

    <template v-else>
      <p
        v-if="msg"
        class="admin-msg"
        :class="{ 'admin-msg--error': msgEsError }"
      >
        {{ msg }}
      </p>

      <section class="admin-card">
        <h2>Nueva categoría</h2>
        <p class="admin-field-hint">
          Las categorías son propias de esta tienda. Luego podés asignarlas al crear productos.
        </p>
        <form class="admin-form" @submit.prevent="crear">
          <label class="admin-field">
            <span>Nombre</span>
            <input v-model="nuevo.nombre" type="text" maxlength="120" required />
          </label>
          <label class="admin-field">
            <span>Orden (opcional)</span>
            <input
              v-model="nuevo.orden"
              type="number"
              min="0"
              step="1"
              placeholder="0"
            />
          </label>
          <button type="submit" class="btn-primary" :disabled="guardando">
            {{ guardando ? 'Guardando…' : 'Crear categoría' }}
          </button>
        </form>
      </section>

      <section v-if="categoriaEdicion" class="admin-card">
        <h2>Editar: {{ categoriaEdicion.nombre }}</h2>
        <form class="admin-form" @submit.prevent="guardarEdicion">
          <label class="admin-field">
            <span>Nombre</span>
            <input v-model="edicion.nombre" type="text" maxlength="120" required />
          </label>
          <label class="admin-field">
            <span>Orden</span>
            <input v-model="edicion.orden" type="number" min="0" step="1" />
          </label>
          <label class="admin-field">
            <span>Estado</span>
            <label style="display: flex; align-items: center; gap: 0.5rem; font-weight: normal">
              <input v-model="edicion.activo" type="checkbox" />
              Activa (visible en la tienda pública)
            </label>
          </label>
          <div style="display: flex; gap: 0.5rem; flex-wrap: wrap">
            <button type="submit" class="btn-primary" :disabled="guardando">
              {{ guardando ? 'Guardando…' : 'Guardar cambios' }}
            </button>
            <button type="button" class="btn-ghost" @click="cerrarEdicion">Cancelar</button>
          </div>
        </form>
      </section>

      <section class="admin-card">
        <h2>Listado</h2>
        <p v-if="!categorias.length" class="admin-msg">
          No hay categorías. Creá la primera arriba.
        </p>
        <div v-else class="admin-table-wrap">
          <table class="admin-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Orden</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in categorias" :key="c.id">
                <td>{{ c.nombre }}</td>
                <td>{{ c.orden }}</td>
                <td>
                  <span class="admin-pill">{{ c.activo ? 'Activa' : 'Inactiva' }}</span>
                </td>
                <td>
                  <button type="button" class="btn-ghost" @click="abrirEdicion(c)">
                    Editar
                  </button>
                  <button
                    type="button"
                    class="btn-ghost"
                    :disabled="operandoId === c.id"
                    @click="eliminar(c)"
                  >
                    Eliminar
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </template>
  </div>
</template>
