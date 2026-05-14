<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AdminNav from '../../components/admin/AdminNav.vue'
import { useAuthedFetch } from '../../composables/useAuthedFetch'
import { useAuthStore } from '../../stores/auth'
import type { CategoriaAdminDto, ProductoAdminDto } from '../../types/api'
import { normalizarProductoAdminDto } from '../../utils/normalizarProductoApi'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const authedFetch = useAuthedFetch()

const slug = computed(() => (route.params.slug as string) || '')

const productos = ref<ProductoAdminDto[]>([])
const categorias = ref<CategoriaAdminDto[]>([])
const cargando = ref(true)
const error = ref<string | null>(null)
const guardando = ref(false)
const formMsg = ref<string | null>(null)

const productoEdicion = ref<ProductoAdminDto | null>(null)
const guardandoEdicion = ref(false)
const operandoId = ref<string | null>(null)
const tablaMsg = ref<string | null>(null)

const precioFmt = new Intl.NumberFormat('es-AR', {
  style: 'currency',
  currency: 'ARS',
  maximumFractionDigits: 2,
})

const nuevo = reactive({
  categoriaId: '' as string,
  nombre: '',
  descripcionCorta: '',
  imagenUrl: '',
  precio: '' as string,
  stock: '0' as string,
})

const edicion = reactive({
  categoriaId: '' as string,
  nombre: '',
  descripcionCorta: '',
  imagenUrl: '',
  precio: '' as string,
  stock: '0' as string,
  activo: true,
})

/** Vista previa URL imagen: reset al cambiar el texto (debe ir después de `nuevo` / `edicion`). */
const nuevoImagenPreviewFallo = ref(false)
const edicionImagenPreviewFallo = ref(false)
watch(
  () => nuevo.imagenUrl,
  () => {
    nuevoImagenPreviewFallo.value = false
  },
)
watch(
  () => edicion.imagenUrl,
  () => {
    edicionImagenPreviewFallo.value = false
  },
)

function trimImagenUrl(s: string) {
  return s
    .trim()
    .replace(/^\u200b+|\u200b+$/g, '')
    .replace(/^['"]+|['"]+$/g, '')
    .trim()
}

function baseUrl() {
  return `/api/negocios/${encodeURIComponent(slug.value)}`
}

function nombreCategoria(categoriaId: string | null | undefined) {
  if (!categoriaId) return '—'
  return categorias.value.find((c) => c.id === categoriaId)?.nombre ?? categoriaId
}

function bodyActualizarDesdeProducto(
  p: ProductoAdminDto,
  patch: Partial<{
    categoriaId: string | null
    nombre: string
    descripcionCorta: string | null
    precio: number
    stock: number
    atributos: Record<string, string> | null
    activo: boolean
    imagenUrl: string | null
  }> = {},
) {
  return {
    nombre: patch.nombre ?? p.nombre,
    descripcionCorta:
      patch.descripcionCorta !== undefined ? patch.descripcionCorta : (p.descripcionCorta ?? null),
    categoriaId:
      patch.categoriaId !== undefined ? patch.categoriaId : (p.categoriaId ?? null),
    precio: patch.precio ?? p.precio,
    stock: patch.stock ?? p.stock,
    atributos: patch.atributos !== undefined ? patch.atributos : (p.atributos ?? null),
    activo: patch.activo ?? p.activo,
    ImagenUrl: patch.imagenUrl !== undefined ? patch.imagenUrl : (p.imagenUrl ?? null),
  }
}

async function manejar401() {
  auth.cerrarSesion()
  await router.replace({
    name: 'admin-login',
    params: { slug: slug.value },
    query: { redirect: route.fullPath },
  })
}

async function cargarTodo() {
  cargando.value = true
  error.value = null
  productos.value = []
  categorias.value = []
  tablaMsg.value = null
  const s = slug.value
  if (!s) {
    error.value = 'Slug inválido.'
    cargando.value = false
    return
  }
  try {
    const [rProd, rCat] = await Promise.all([
      authedFetch(`${baseUrl()}/admin/productos`),
      authedFetch(`${baseUrl()}/admin/categorias`),
    ])
    if (rProd.status === 401 || rCat.status === 401) {
      await manejar401()
      return
    }
    if (rProd.status === 403 || rCat.status === 403) {
      error.value = 'No tenés permiso para este negocio.'
      return
    }
    if (!rProd.ok) {
      error.value = `Error productos ${rProd.status}`
      return
    }
    if (!rCat.ok) {
      error.value = `Error categorías ${rCat.status}`
      return
    }
    const rawProds = (await rProd.json()) as unknown
    productos.value = Array.isArray(rawProds)
      ? rawProds.map((x) => normalizarProductoAdminDto(x as Record<string, unknown>))
      : []
    categorias.value = (await rCat.json()) as CategoriaAdminDto[]
    if (productoEdicion.value) {
      const actualizado = productos.value.find((x) => x.id === productoEdicion.value!.id)
      productoEdicion.value = actualizado ?? null
      if (actualizado) sincronizarEdicionDesdeProducto(actualizado)
    }
  } catch {
    error.value = 'Error de red al cargar catálogo.'
  } finally {
    cargando.value = false
  }
}

function sincronizarEdicionDesdeProducto(p: ProductoAdminDto) {
  edicion.categoriaId = p.categoriaId ?? ''
  edicion.nombre = p.nombre
  edicion.descripcionCorta = p.descripcionCorta ?? ''
  edicion.imagenUrl = p.imagenUrl ?? ''
  edicion.precio = String(p.precio)
  edicion.stock = String(p.stock)
  edicion.activo = p.activo
}

function abrirEdicion(p: ProductoAdminDto) {
  tablaMsg.value = null
  edicionImagenPreviewFallo.value = false
  productoEdicion.value = p
  sincronizarEdicionDesdeProducto(p)
}

function cerrarEdicion() {
  productoEdicion.value = null
  guardandoEdicion.value = false
}

async function guardarEdicion() {
  const p = productoEdicion.value
  if (!p) return
  tablaMsg.value = null
  guardandoEdicion.value = true
  const precio = Number(edicion.precio.replace(',', '.'))
  const stock = Number.parseInt(edicion.stock, 10)
  if (!edicion.nombre.trim()) {
    tablaMsg.value = 'Completá el nombre.'
    guardandoEdicion.value = false
    return
  }
  if (!Number.isFinite(precio) || precio < 0) {
    tablaMsg.value = 'Precio inválido.'
    guardandoEdicion.value = false
    return
  }
  if (!Number.isFinite(stock) || stock < 0) {
    tablaMsg.value = 'Stock inválido.'
    guardandoEdicion.value = false
    return
  }
  const body = bodyActualizarDesdeProducto(p, {
    nombre: edicion.nombre.trim(),
    descripcionCorta: edicion.descripcionCorta.trim() || null,
    categoriaId: edicion.categoriaId ? edicion.categoriaId : null,
    precio,
    stock,
    activo: edicion.activo,
    imagenUrl: trimImagenUrl(edicion.imagenUrl) || null,
  })
  try {
    const res = await authedFetch(`${baseUrl()}/admin/productos/${encodeURIComponent(p.id)}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    if (res.status === 401) {
      await manejar401()
      return
    }
    if (!res.ok) {
      let detalle = `Error ${res.status}`
      try {
        const j = (await res.json()) as { error?: string }
        if (j.error) detalle = j.error
      } catch {
        /* ignore */
      }
      tablaMsg.value = detalle
      return
    }
    tablaMsg.value = 'Cambios guardados.'
    await cargarTodo()
  } catch {
    tablaMsg.value = 'Error de red al guardar.'
  } finally {
    guardandoEdicion.value = false
  }
}

async function alternarActivo(p: ProductoAdminDto) {
  tablaMsg.value = null
  operandoId.value = p.id
  const body = bodyActualizarDesdeProducto(p, { activo: !p.activo })
  try {
    const res = await authedFetch(`${baseUrl()}/admin/productos/${encodeURIComponent(p.id)}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    if (res.status === 401) {
      await manejar401()
      return
    }
    if (!res.ok) {
      tablaMsg.value = `No se pudo cambiar estado (${res.status}).`
      return
    }
    tablaMsg.value = p.activo ? 'Producto desactivado.' : 'Producto activado.'
    await cargarTodo()
  } catch {
    tablaMsg.value = 'Error de red.'
  } finally {
    operandoId.value = null
  }
}

async function eliminarProducto(p: ProductoAdminDto) {
  if (!confirm(`¿Eliminar el producto "${p.nombre}"? Esta acción no se puede deshacer.`)) return
  tablaMsg.value = null
  operandoId.value = p.id
  try {
    const res = await authedFetch(`${baseUrl()}/admin/productos/${encodeURIComponent(p.id)}`, {
      method: 'DELETE',
    })
    if (res.status === 401) {
      await manejar401()
      return
    }
    if (res.status === 404) {
      tablaMsg.value = 'El producto ya no existe.'
      await cargarTodo()
      return
    }
    if (!res.ok) {
      tablaMsg.value = `Error al eliminar (${res.status}).`
      return
    }
    if (productoEdicion.value?.id === p.id) cerrarEdicion()
    tablaMsg.value = 'Producto eliminado.'
    await cargarTodo()
  } catch {
    tablaMsg.value = 'Error de red al eliminar.'
  } finally {
    operandoId.value = null
  }
}

async function crearProducto() {
  formMsg.value = null
  guardando.value = true
  const precio = Number(nuevo.precio.replace(',', '.'))
  const stock = Number.parseInt(nuevo.stock, 10)
  if (!nuevo.nombre.trim()) {
    formMsg.value = 'Completá el nombre.'
    guardando.value = false
    return
  }
  if (!Number.isFinite(precio) || precio < 0) {
    formMsg.value = 'Precio inválido.'
    guardando.value = false
    return
  }
  if (!Number.isFinite(stock) || stock < 0) {
    formMsg.value = 'Stock inválido.'
    guardando.value = false
    return
  }
  try {
    const body: Record<string, unknown> = {
      nombre: nuevo.nombre.trim(),
      precio,
      stock,
    }
    if (nuevo.descripcionCorta.trim()) body.descripcionCorta = nuevo.descripcionCorta.trim()
    if (nuevo.categoriaId) body.categoriaId = nuevo.categoriaId
    const urlImg = trimImagenUrl(nuevo.imagenUrl)
    if (urlImg) body.ImagenUrl = urlImg

    const res = await authedFetch(`${baseUrl()}/admin/productos`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    if (res.status === 401) {
      await manejar401()
      return
    }
    if (!res.ok) {
      let detalle = `Error ${res.status}`
      try {
        const j = (await res.json()) as { error?: string }
        if (j.error) detalle = j.error
      } catch {
        /* ignore */
      }
      formMsg.value = detalle
      return
    }
    nuevo.nombre = ''
    nuevo.descripcionCorta = ''
    nuevo.imagenUrl = ''
    nuevo.precio = ''
    nuevo.stock = '0'
    nuevo.categoriaId = ''
    formMsg.value = 'Producto creado.'
    await cargarTodo()
  } catch {
    formMsg.value = 'Error de red al guardar.'
  } finally {
    guardando.value = false
  }
}

function salir() {
  auth.cerrarSesion()
  void router.push({ name: 'admin-login', params: { slug: slug.value } })
}

/** Solo estos se muestran como éxito (verde); el resto va en rojo. */
const mensajesFormOk = new Set(['Producto creado.'])
const formMsgEsError = computed(() => {
  const m = formMsg.value
  if (!m) return false
  return !mensajesFormOk.has(m)
})

const mensajesTablaOk = new Set([
  'Cambios guardados.',
  'Producto desactivado.',
  'Producto activado.',
  'Producto eliminado.',
])
const tablaMsgEsError = computed(() => {
  const m = tablaMsg.value
  if (!m) return false
  return !mensajesTablaOk.has(m)
})

onMounted(() => {
  void cargarTodo()
})
</script>

<template>
  <div class="admin-page">
    <header class="admin-head">
      <div>
        <h1>Productos</h1>
        <p class="admin-sub">
          Tienda <code>{{ slug }}</code>
          <span v-if="auth.usuario"> · {{ auth.usuario.email }} ({{ auth.usuario.rol }})</span>
        </p>
      </div>
      <div class="admin-actions">
        <button type="button" class="btn-ghost" @click="cargarTodo">Actualizar</button>
        <button type="button" class="btn-ghost" @click="salir">Salir</button>
      </div>
    </header>

    <AdminNav :slug="slug" />

    <nav class="admin-breadcrumb">
      <RouterLink to="/">Inicio</RouterLink>
      <span class="sep">·</span>
      <RouterLink :to="{ name: 'tienda', params: { slug } }">Ver tienda pública</RouterLink>
    </nav>

    <p v-if="cargando" class="admin-msg">Cargando…</p>
    <p v-else-if="error" class="admin-msg admin-msg--error">{{ error }}</p>

    <template v-else>
      <section class="admin-card">
        <h2>Nuevo producto</h2>
        <form class="admin-form" @submit.prevent="crearProducto">
          <label class="admin-field">
            <span>Nombre</span>
            <input v-model="nuevo.nombre" type="text" maxlength="200" required />
          </label>
          <label class="admin-field">
            <span>Categoría</span>
            <select v-model="nuevo.categoriaId">
              <option value="">(ninguna)</option>
              <option v-for="c in categorias" :key="c.id" :value="c.id" :disabled="!c.activo">
                {{ c.nombre }}{{ c.activo ? '' : ' (inactiva)' }}
              </option>
            </select>
          </label>
          <label class="admin-field">
            <span>Descripción corta</span>
            <input v-model="nuevo.descripcionCorta" type="text" maxlength="2000" />
          </label>
          <div class="admin-field-wrap">
            <label class="admin-field">
              <span>URL de la imagen</span>
              <input
                id="nuevo-imagen-url"
                v-model="nuevo.imagenUrl"
                type="text"
                inputmode="url"
                maxlength="2048"
                placeholder="https://ejemplo.com/foto.jpg"
                spellcheck="false"
                autocomplete="off"
              />
            </label>
            <p class="admin-field-hint">
              Pegá el enlace directo al archivo (termina en .jpg, .png, etc.). Los enlaces de
              «compartir carpeta» de Drive suelen no servir. Para probar:
              <code>https://picsum.photos/400/300</code>
            </p>
            <div v-if="trimImagenUrl(nuevo.imagenUrl)" class="admin-imagen-preview">
              <p class="admin-field-hint admin-field-hint--tight">Vista previa</p>
              <img
                class="admin-preview-img"
                :src="trimImagenUrl(nuevo.imagenUrl)"
                alt=""
                @error="nuevoImagenPreviewFallo = true"
                @load="nuevoImagenPreviewFallo = false"
              />
              <p v-if="nuevoImagenPreviewFallo" class="admin-imagen-preview-err">
                No se pudo mostrar: la URL no es una imagen pública o el sitio la bloquea. Copiá la
                dirección de la imagen (no la de la página).
              </p>
            </div>
          </div>
          <div class="admin-form-row">
            <label class="admin-field">
              <span>Precio</span>
              <input v-model="nuevo.precio" type="text" inputmode="decimal" placeholder="0" />
            </label>
            <label class="admin-field">
              <span>Stock</span>
              <input v-model="nuevo.stock" type="number" min="0" step="1" />
            </label>
          </div>
          <button type="submit" class="btn-primary" :disabled="guardando">
            {{ guardando ? 'Guardando…' : 'Crear' }}
          </button>
          <p
            v-if="formMsg"
            class="admin-form-msg"
            :class="{ 'admin-form-msg--error': formMsgEsError }"
          >
            {{ formMsg }}
          </p>
        </form>
      </section>

      <h2 class="admin-section-title">Listado</h2>
      <p
        v-if="tablaMsg"
        class="admin-msg tabla-feedback"
        :class="{ 'admin-msg--error': tablaMsgEsError }"
      >
        {{ tablaMsg }}
      </p>

      <div v-if="!productos.length" class="admin-msg">No hay productos.</div>
      <div v-else class="admin-table-wrap">
        <table class="admin-table admin-table--acciones">
          <thead>
            <tr>
              <th class="col-thumb">Foto</th>
              <th>Nombre</th>
              <th>Categoría</th>
              <th>Precio</th>
              <th>Stock</th>
              <th>Activo</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in productos" :key="p.id">
              <td class="col-thumb">
                <img
                  v-if="p.imagenUrl"
                  class="admin-thumb"
                  :src="p.imagenUrl"
                  alt=""
                  loading="lazy"
                />
                <span v-else class="admin-thumb-placeholder">—</span>
              </td>
              <td>{{ p.nombre }}</td>
              <td>{{ nombreCategoria(p.categoriaId) }}</td>
              <td>{{ precioFmt.format(p.precio) }}</td>
              <td>{{ p.stock }}</td>
              <td>
                <span class="admin-pill">{{ p.activo ? 'Sí' : 'No' }}</span>
              </td>
              <td class="celda-acciones">
                <div class="fila-acciones">
                  <button
                    type="button"
                    class="btn-mini"
                    :disabled="operandoId === p.id"
                    @click="abrirEdicion(p)"
                  >
                    Editar
                  </button>
                  <button
                    type="button"
                    class="btn-mini btn-mini--muted"
                    :disabled="operandoId === p.id"
                    @click="alternarActivo(p)"
                  >
                    {{ p.activo ? 'Desactivar' : 'Activar' }}
                  </button>
                  <button
                    type="button"
                    class="btn-mini btn-mini--danger"
                    :disabled="operandoId === p.id"
                    @click="eliminarProducto(p)"
                  >
                    Eliminar
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="productoEdicion" class="admin-card panel-edicion">
        <div class="panel-edicion__head">
          <h2>Editar producto</h2>
          <button type="button" class="btn-ghost" @click="cerrarEdicion">Cerrar</button>
        </div>
        <p class="admin-sub panel-edicion__id"><code>{{ productoEdicion.id }}</code></p>
        <form class="admin-form admin-form--stretch" @submit.prevent="guardarEdicion">
          <label class="admin-field">
            <span>Nombre</span>
            <input v-model="edicion.nombre" type="text" maxlength="200" required />
          </label>
          <label class="admin-field">
            <span>Categoría</span>
            <select v-model="edicion.categoriaId">
              <option value="">(ninguna)</option>
              <option v-for="c in categorias" :key="c.id" :value="c.id" :disabled="!c.activo">
                {{ c.nombre }}{{ c.activo ? '' : ' (inactiva)' }}
              </option>
            </select>
          </label>
          <label class="admin-field">
            <span>Descripción corta</span>
            <input v-model="edicion.descripcionCorta" type="text" maxlength="2000" />
          </label>
          <div class="admin-field-wrap">
            <label class="admin-field">
              <span>URL de la imagen</span>
              <input
                id="edicion-imagen-url"
                v-model="edicion.imagenUrl"
                type="text"
                inputmode="url"
                maxlength="2048"
                placeholder="https://ejemplo.com/foto.jpg"
                spellcheck="false"
                autocomplete="off"
              />
            </label>
            <p class="admin-field-hint">Vacío = sin foto en la tienda.</p>
            <div v-if="trimImagenUrl(edicion.imagenUrl)" class="admin-imagen-preview">
              <p class="admin-field-hint admin-field-hint--tight">Vista previa</p>
              <img
                class="admin-preview-img"
                :src="trimImagenUrl(edicion.imagenUrl)"
                alt=""
                @error="edicionImagenPreviewFallo = true"
                @load="edicionImagenPreviewFallo = false"
              />
              <p v-if="edicionImagenPreviewFallo" class="admin-imagen-preview-err">
                No se pudo mostrar: revisá que sea enlace directo a la imagen.
              </p>
            </div>
          </div>
          <div class="admin-form-row">
            <label class="admin-field">
              <span>Precio</span>
              <input v-model="edicion.precio" type="text" inputmode="decimal" />
            </label>
            <label class="admin-field">
              <span>Stock</span>
              <input v-model="edicion.stock" type="number" min="0" step="1" />
            </label>
          </div>
          <label class="admin-field admin-field--inline">
            <input v-model="edicion.activo" type="checkbox" />
            <span>Producto activo (visible en tienda pública)</span>
          </label>
          <div class="panel-edicion__actions">
            <button type="submit" class="btn-primary" :disabled="guardandoEdicion">
              {{ guardandoEdicion ? 'Guardando…' : 'Guardar cambios' }}
            </button>
          </div>
        </form>
      </div>
    </template>
  </div>
</template>

<style scoped>
.tabla-feedback {
  margin-bottom: 0.75rem;
}
.admin-field-wrap {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  max-width: 28rem;
}
.admin-form--stretch .admin-field-wrap {
  max-width: none;
}
.admin-field-hint {
  font-size: 0.78rem;
  color: var(--text-muted, #6b7280);
  margin: 0;
  line-height: 1.4;
}
.admin-field-hint code {
  font-size: 0.72rem;
  word-break: break-all;
}
.admin-field-hint--tight {
  margin: 0;
}
.col-thumb {
  width: 3.25rem;
  text-align: center;
}
.admin-thumb {
  width: 2.5rem;
  height: 2.5rem;
  object-fit: cover;
  border-radius: 6px;
  vertical-align: middle;
  border: 1px solid var(--border, #e5e7eb);
}
.admin-thumb-placeholder {
  color: var(--text-muted, #9ca3af);
  font-size: 0.85rem;
}
.admin-imagen-preview {
  margin-top: 0.5rem;
}
.admin-preview-img {
  display: block;
  max-width: 12rem;
  max-height: 8rem;
  margin-top: 0.35rem;
  border-radius: 8px;
  border: 1px solid var(--border, #e5e7eb);
  object-fit: contain;
  background: var(--code-bg, #f3f4f6);
}
.admin-imagen-preview-err {
  margin: 0.4rem 0 0;
  font-size: 0.8rem;
  color: #b45309;
  line-height: 1.35;
}
.celda-acciones {
  vertical-align: middle;
}
.fila-acciones {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  align-items: center;
}
.btn-mini {
  padding: 0.28rem 0.55rem;
  border-radius: 8px;
  border: 1px solid var(--border);
  background: var(--bg);
  color: var(--text-h);
  font-size: 0.78rem;
  font-weight: 500;
  cursor: pointer;
}
.btn-mini:hover:not(:disabled) {
  background: var(--code-bg);
}
.btn-mini:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.btn-mini--muted {
  color: var(--text);
}
.btn-mini--danger {
  border-color: rgba(185, 28, 28, 0.45);
  color: #b91c1c;
}
.btn-mini--danger:hover:not(:disabled) {
  background: rgba(185, 28, 28, 0.08);
}
.panel-edicion {
  margin-top: 1.5rem;
}
.panel-edicion__head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
  margin-bottom: 0.35rem;
}
.panel-edicion__head h2 {
  margin: 0;
}
.panel-edicion__id {
  margin: 0 0 1rem;
  font-size: 0.8rem;
}
.panel-edicion__actions {
  margin-top: 0.25rem;
}
.admin-field--inline {
  flex-direction: row;
  align-items: center;
  gap: 0.5rem;
}
.admin-field--inline span {
  font-weight: 400;
}
@media (min-width: 56rem) {
  .admin-table--acciones {
    font-size: 0.88rem;
  }
}
</style>
