# MarketSaaS — Tesis (T.U.P.)

Backend **.NET 8** + **MongoDB** + **JWT** y frontend **Vue 3 + Vite** para la plataforma multi-tenant del trabajo final.

## Estructura

```
.
├── MarketSaaS.sln
├── src/MarketSaaS.Api/       # API REST
├── src/MarketSaaS.Web/       # SPA Vue 3 + TypeScript (Vite)
└── README.md
```

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MongoDB](https://www.mongodb.com/try/download/community) en ejecución (por defecto `mongodb://localhost:27017`)
- [Node.js](https://nodejs.org/) LTS (para el frontend)

## MongoDB (local)

1. **Instalá** MongoDB Community y dejá el servicio **MongoDB** iniciado (Servicios de Windows), o ejecutá `mongod` manualmente.
2. **No hace falta** crear la base “a mano”: la API usa el nombre `DatabaseName` de `appsettings.json` (por defecto `marketsaas`). Al **primer arranque** se crean índices; al **primer insert** (usuario, negocio, pedido, etc.) aparecen las **colecciones** en la base.
3. Opcional: [MongoDB Compass](https://www.mongodb.com/products/compass) → conectar a `mongodb://localhost:27017` → ver la base `marketsaas` después de usar la API.
4. Si la API **no arranca** o falla al inicio con error de conexión, Mongo no está escuchando o la **connection string** no coincide con tu instalación.

## Convenciones de código (para quien mantenga el repo)

- **Dominio en español** en nombres públicos de la API (rutas, mensajes, propiedades JSON donde aplica): `Negocio`, `Pedido`, `CrearPedidoRequest`, etc.
- **Parámetros de entrada** a controladores: preferimos `solicitud` para el cuerpo (`[FromBody]`) y nombres completos en variables locales (`negocio`, `producto`, `pedido`) en lugar de `n`, `p`, `dto`.
- **Tenant en admin:** el filtro `[RequireMatchingNegocio]` guarda el negocio en `HttpContext.Items`; leelo con `HttpContext.TryGetNegocioActual(out var negocio)` (`HttpContextNegocioExtensions`).
- **`CancellationToken`:** abreviatura `ct` en firmas (convención habitual en .NET).

## Configuración

Editá `src/MarketSaaS.Api/appsettings.json` o usá **User Secrets** (recomendado para la clave JWT):

```bash
cd src/MarketSaaS.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "tu_clave_super_secreta_de_al_menos_32_caracteres"
```

En **GitHub** no subas claves reales: usá **Secrets** en el servidor o variables de entorno (`Jwt__SigningKey`).

## Ejecutar

```bash
cd src/MarketSaaS.Api
dotnet run
```

- Swagger (desarrollo): `https://localhost:{puerto}/swagger`

### Frontend (Vue)

```bash
cd src/MarketSaaS.Web
npm install
npm run dev
```

Con la API en **`http://localhost:5037`**, Vite proxifica `/api` hacia el backend. Detalle: `src/MarketSaaS.Web/README.md`.

## Endpoints iniciales

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/negocios` | Crear negocio — **JWT** rol `SuperAdmin`, slug único |
| GET | `/api/negocios/{slug}` | Obtener negocio por slug (público) |
| GET | `/api/negocios/{slug}/admin/contexto` | **JWT** `SuperAdmin` o `AdminTienda`; el `negocio_id` del token debe coincidir con el slug (excepto SuperAdmin) |
| POST | `/api/auth/registro` | Registro: `SuperAdmin` solo si aún no hay ninguno en BD; `AdminTienda`/`Cliente` con `negocioId` |
| POST | `/api/auth/login` | Login → JWT |
| GET | `/api/auth/me` | Perfil (requiere `Authorization: Bearer …`) |
| GET | `/api/negocios/{slug}/categorias` | Listar categorías activas (público) |
| GET | `/api/negocios/{slug}/categorias/{id}` | Categoría activa por id (público) |
| GET | `/api/negocios/{slug}/productos` | Listar productos activos; query `?categoriaId=` opcional (público) |
| GET | `/api/negocios/{slug}/productos/{id}` | Producto activo por id (público) |
| GET/POST/PUT/DELETE | `/api/negocios/{slug}/admin/categorias` | CRUD categorías — **JWT** + `[RequireMatchingNegocio]` |
| GET/POST/PUT/DELETE | `/api/negocios/{slug}/admin/productos` | CRUD productos (precio, stock, `atributos` opcional) — **JWT** + tenant |
| POST | `/api/negocios/{slug}/pedidos` | Crear pedido **PendientePago**: valida stock y totales; el stock se descuenta al aprobar el pago (webhook MP) |
| POST | `/api/negocios/{slug}/pedidos/{pedidoId}/mercadopago/preferencia` | Preferencia Checkout Pro (público) |
| GET | `/api/negocios/{slug}/admin/pedidos` | Listar pedidos del negocio (`?limite=`, máx. 500) — **JWT** + tenant |
| GET | `/api/negocios/{slug}/admin/pedidos/{id}` | Detalle de pedido — **JWT** + tenant |

Roles: `SuperAdmin`, `AdminTienda`, `Cliente`. Políticas en `Authorization/Policies.cs`; aislamiento por slug en `[RequireMatchingNegocio]`.

**Pedidos y pago:** flujo **PendientePago** → preferencia Mercado Pago → **webhook** aprueba → descuento de stock atómico. Estado legacy **Confirmado** puede existir en datos viejos.

## Git + GitHub

1. Creá un repositorio vacío en GitHub (sin README si ya tenés uno local).

2. En la carpeta `TESIS`:

```bash
git init
git add .
git commit -m "Initial: MarketSaaS API, MongoDB, JWT"
git branch -M main
git remote add origin https://github.com/TU_USUARIO/TU_REPO.git
git push -u origin main
```

3. Convención de commits: mensajes claros en español o inglés, un cambio lógico por commit cuando puedas.

## Próximos pasos sugeridos

- **MarketSaaS.Web**: router por `slug`, catálogo, carrito (Pinia), checkout MP, panel admin con JWT.
- **SignalR** (chat cliente–tienda) y **dashboard** de métricas.
- Invitación de **otros SuperAdmin** (endpoint protegido) si hace falta más de un administrador de plataforma.
