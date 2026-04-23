<script setup lang="ts">
import { apiUrl } from './config/api'

/** Ejemplo opcional: probá con un slug real de tu MongoDB. */
const slugDemo = 'demo'

async function probarApi() {
  const res = await fetch(apiUrl(`/api/negocios/${slugDemo}`))
  if (res.ok) {
    const data = await res.json()
    alert(`Negocio: ${data.nombre ?? data.slug ?? JSON.stringify(data)}`)
  } else if (res.status === 404) {
    alert(`No hay negocio con slug "${slugDemo}". Cambiá slugDemo en App.vue o creá el negocio en la API.`)
  } else {
    alert(`Error ${res.status}: ¿La API está en http://localhost:5037?`)
  }
}
</script>

<template>
  <main class="wrap">
    <h1>MarketSaaS</h1>
    <p class="lead">
      Frontend Vue 3 + Vite + TypeScript. En desarrollo, las llamadas a <code>/api</code> se proxifican al backend
      (.NET en el puerto 5037).
    </p>
    <p class="hint">Próximo paso: router por <code>slug</code>, Pinia (carrito) y pantallas tienda / admin.</p>
    <button type="button" class="btn" @click="probarApi">Probar GET negocio (demo)</button>
  </main>
</template>

<style scoped>
.wrap {
  max-width: 40rem;
  margin: 2rem auto;
  padding: 0 1rem;
  font-family: system-ui, sans-serif;
}
h1 {
  font-size: 1.75rem;
}
.lead {
  color: #374151;
  line-height: 1.5;
}
.hint {
  font-size: 0.9rem;
  color: #6b7280;
}
code {
  font-size: 0.85em;
  background: #f3f4f6;
  padding: 0.1em 0.35em;
  border-radius: 4px;
}
.btn {
  margin-top: 1rem;
  padding: 0.5rem 1rem;
  border-radius: 8px;
  border: 1px solid #2563eb;
  background: #2563eb;
  color: #fff;
  font-size: 1rem;
  cursor: pointer;
}
.btn:hover {
  background: #1d4ed8;
}
</style>
