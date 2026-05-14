<script setup lang="ts">
import { HubConnectionState } from '@microsoft/signalr'
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { createChatHub, type ChatMensajeDto } from '../../composables/useChatHub'

const props = defineProps<{ slug: string }>()

const nombre = ref('Cliente')
const texto = ref('')
const mensajes = ref<ChatMensajeDto[]>([])
const enviando = ref(false)
const estado = ref<'conectando' | 'conectado' | 'error'>('conectando')

const hub = createChatHub({
  onHistorial: (items) => {
    mensajes.value = items
  },
  onMensaje: (msg) => {
    mensajes.value = [...mensajes.value, msg]
  },
})

const conectado = computed(() => estado.value === 'conectado')

async function conectar() {
  if (!props.slug) return
  estado.value = 'conectando'
  try {
    if (hub.state === HubConnectionState.Disconnected) {
      await hub.start()
    }
    await hub.invoke('Unirse', props.slug)
    estado.value = 'conectado'
  } catch {
    estado.value = 'error'
  }
}

async function enviar() {
  const t = texto.value.trim()
  if (!t || !props.slug || !conectado.value) return

  enviando.value = true
  try {
    await hub.invoke('EnviarMensaje', props.slug, {
      nombre: nombre.value.trim() || 'Cliente',
      texto: t,
    })
    texto.value = ''
  } catch {
    estado.value = 'error'
  } finally {
    enviando.value = false
  }
}

watch(
  () => props.slug,
  async (slugActual, slugAnterior) => {
    if (!slugActual) return
    if (slugAnterior && slugActual !== slugAnterior) {
      mensajes.value = []
    }
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
  <section class="chat-widget">
    <div class="chat-widget__head">
      <strong>Chat con la tienda</strong>
      <span :class="['chat-widget__status', `is-${estado}`]">
        {{ estado === 'conectado' ? 'online' : estado === 'conectando' ? 'conectando…' : 'error' }}
      </span>
    </div>

    <label class="chat-widget__label">
      Tu nombre
      <input v-model="nombre" type="text" maxlength="80" />
    </label>

    <div class="chat-widget__log">
      <p v-if="!mensajes.length" class="chat-widget__empty">Todavía no hay mensajes.</p>
      <div v-for="m in mensajes" :key="m.id" class="chat-widget__msg" :class="`from-${m.remitenteTipo}`">
        <div class="chat-widget__meta">
          <strong>{{ m.remitenteNombre }}</strong>
          <span>{{ new Date(m.enviadoEn).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' }) }}</span>
        </div>
        <p>{{ m.texto }}</p>
      </div>
    </div>

    <form class="chat-widget__composer" @submit.prevent="enviar">
      <input
        v-model="texto"
        type="text"
        maxlength="1000"
        placeholder="Escribí un mensaje..."
        :disabled="!conectado || enviando"
      />
      <button type="submit" :disabled="!texto.trim() || !conectado || enviando">Enviar</button>
    </form>
  </section>
</template>

<style scoped>
.chat-widget { margin-top: 1.25rem; border: 1px solid var(--border); border-radius: 12px; padding: 0.9rem; background: var(--bg); }
.chat-widget__head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.65rem; }
.chat-widget__status { font-size: 0.75rem; font-weight: 600; }
.is-conectado { color: #16a34a; }
.is-conectando { color: #ca8a04; }
.is-error { color: #dc2626; }
.chat-widget__label { display: block; font-size: 0.8rem; color: var(--text); margin-bottom: 0.65rem; }
.chat-widget__label input { width: 100%; margin-top: 0.25rem; padding: 0.4rem 0.5rem; border-radius: 8px; border: 1px solid var(--border); }
.chat-widget__log { max-height: 16rem; overflow: auto; border: 1px solid var(--border); border-radius: 8px; padding: 0.55rem; background: var(--code-bg); }
.chat-widget__empty { margin: 0; color: var(--text); font-size: 0.85rem; }
.chat-widget__msg { margin-bottom: 0.5rem; padding: 0.45rem 0.55rem; border-radius: 8px; background: var(--bg); }
.chat-widget__msg p { margin: 0.15rem 0 0; }
.chat-widget__meta { display: flex; justify-content: space-between; font-size: 0.75rem; color: var(--text); }
.chat-widget__composer { margin-top: 0.65rem; display: grid; grid-template-columns: 1fr auto; gap: 0.5rem; }
.chat-widget__composer input { padding: 0.45rem 0.55rem; border-radius: 8px; border: 1px solid var(--border); }
.chat-widget__composer button { border: none; border-radius: 8px; padding: 0.45rem 0.8rem; background: var(--accent); color: #fff; }
</style>

