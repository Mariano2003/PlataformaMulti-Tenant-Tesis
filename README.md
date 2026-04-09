# MarketSaaS — Tesis (T.U.P.)

Backend **.NET 8** + **MongoDB** + **JWT** para la plataforma multi-tenant del trabajo final.

## Estructura

```
.
├── MarketSaaS.sln
├── src/MarketSaaS.Api/    # API REST
└── README.md
```

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MongoDB](https://www.mongodb.com/try/download/community) en ejecución (por defecto `mongodb://localhost:27017`)

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

Roles: `SuperAdmin`, `AdminTienda`, `Cliente`. Políticas en `Authorization/Policies.cs`; aislamiento por slug en `[RequireMatchingNegocio]`.

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

- **Checkout / pedidos** y descuento transaccional de stock al confirmar pago (Mercado Pago + webhooks).
- **Frontend Vue** (catálogo por slug, carrito Pinia, panel admin).
- Invitación de **otros SuperAdmin** (endpoint protegido) si hace falta más de un administrador de plataforma.
