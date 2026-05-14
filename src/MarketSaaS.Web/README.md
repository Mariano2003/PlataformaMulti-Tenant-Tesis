# MarketSaaS.Web

SPA **Vue 3** + **Vite** + **TypeScript** para el trabajo final MarketSaaS.

## Requisitos

- [Node.js](https://nodejs.org/) LTS (incluye `npm`)

## Desarrollo

1. Levantá la API .NET (por defecto `http://localhost:5037`).
2. En esta carpeta:

```bash
npm install
npm run dev
```

Abrí la URL que muestra Vite (suele ser `http://localhost:5173`). Las peticiones a `/api` se reenvían al backend (ver `vite.config.ts`).

Si usás la API con **otro puerto**, cambiá `server.proxy['/api'].target` en `vite.config.ts`.

## Producción

Definí `VITE_API_BASE_URL` con la URL pública de la API (sin barra final) y generá el build:

```bash
npm run build
```

Los archivos estáticos quedan en `dist/` (servilos con nginx, Azure Static Web Apps, S3, etc.).

## Variables de entorno

Copiá `.env.example` a `.env` o `.env.production` y ajustá según corresponda.
