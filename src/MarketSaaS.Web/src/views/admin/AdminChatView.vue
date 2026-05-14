<script setup lang="ts">
import { HubConnectionState } from '@microsoft/signalr'
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import AdminNav from '../../components/admin/AdminNav.vue'
import { createChatHub, type ChatMensajeDto } from '../../composables/useChatHub'
import { useAuthStore } from '../../stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const slug = computed(() => (route.params.slug as string) || '')
const texto = ref('')
const mensajes = ref<ChatMensajeDto[]>([])
const estado = ref<'conectando' | 'conectado' | 'error'>('conectando')
const enviando = ref(false)

const hub = createChatHub({
  token: auth.token,
  onHistorial: (items) => {
    mensajes.value = items
  },
  onMensaje: (msg) => {
    mensajes.value = [...mensajes.value, msg]
  },
})

async function conectar() {
  if (!slug.value || !auth.token) return
  estado.value = 'conectando'
  try {
    if (hub.state === HubConnectionState.Disconnected) {
      await hub.start()
    }
    await hub.invoke('Unirse', slug.value)
    estado.value = 'conectado'
  } catch {
    estado.value = 'error'
  }
}

async function enviar() {
  const t = texto.value.trim()
  if (!t || estado.value !== 'conectado') return
  enviando.value = true
  try {
    await hub.invoke('EnviarMensaje', slug.value, {
      nombre: auth.usuario?.nombre || auth.usuario?.email || 'Admin',
      texto: t,
    })
    texto.value = ''
  } catch {
    estado.value = 'error'
  } finally {
    enviando.value = false
  }
}

function salir() {
  auth.cerrarSesion()
  void router.push({ name: 'admin-login', params: { slug: slug.value } })
}

watch(
  () => slug.value,
  async () => {
    mensajes.value = []
    await conectar()
  },
  { immediate: true },
)

onMounted(() => {
  void conectar()
})

onUnmounted(() => {
  void hub.stop()
})
</script>

<template>
  <div class="admin-page admin-page--wide">
    <header class="admin-head">
      <div>
        <h1>Chat en vivo</h1>
        <p class="admin-sub">Tienda <code>{{ slug }}</code></p>
      </div>
      <div class="admin-actions">
        <button type="button" class="btn-ghost" @click="conectar">Reconectar</button>
        <button type="button" class="btn-ghost" @click="salir">Salir</button>
      </div>
    </header>

    <AdminNav :slug="slug" />

    <nav class="admin-breadcrumb">
      <RouterLink to="/">Inicio</RouterLink>
      <span class="sep">·</span>
      <RouterLink :to="{ name: 'tienda', params: { slug } }">Ver tienda pública</RouterLink>
    </nav>

    <p class="admin-msg" :class="{ 'admin-msg--error': estado === 'error' }">
      Estado: {{ estado === 'conectado' ? 'online' : estado === 'conectando' ? 'conectando…' : 'error de conexión' }}
    </p>

    <section class="admin-card">
      <div class="chat-admin-log">
        <p v-if="!mensajes.length" class="chat-empty">Sin mensajes todavía.</p>
        <article v-for="m in mensajes" :key="m.id" class="chat-admin-msg" :class="`from-${m.remitenteTipo}`">
          <header>
            <strong>{{ m.remitenteNombre }}</strong>
            <time>{{ new Date(m.enviadoEn).toLocaleString('es-AR') }}</time>
          </header>
          <p>{{ m.texto }}</p>
        </article>
      </div>

      <form class="chat-admin-send" @submit.prevent="enviar">
        <input
          v-model="texto"
          type="text"
          maxlength="1000"
          placeholder="Responder al cliente..."
          :disabled="estado !== 'conectado' || enviando"
        />
        <button type="submit" class="btn-primary" :disabled="!texto.trim() || estado !== 'conectado' || enviando">
          Enviar
        </button>
      </form>
    </section>
  </div>
</template>

<style scoped>
.chat-admin-log { max-height: 24rem; overflow: auto; border: 1px solid var(--border); border-radius: 10px; padding: 0.7rem; background: var(--bg); }
.chat-empty { margin: 0; color: var(--text); }
.chat-admin-msg { padding: 0.55rem 0.65rem; border-radius: 9px; background: var(--code-bg); margin-bottom: 0.5rem; }
.chat-admin-msg header { display: flex; justify-content: space-between; font-size: 0.75rem; color: var(--text); }
.chat-admin-msg p { margin: 0.25rem 0 0; }
.chat-admin-msg.from-admin { border-left: 3px solid var(--accent); }
.chat-admin-send { margin-top: 0.75rem; display: grid; grid-template-columns: 1fr auto; gap: 0.6rem; }
.chat-admin-send input { border: 1px solid var(--border); border-radius: 10px; padding: 0.55rem 0.65rem; }
</style>

