# MarketSaaS — Tesis (T.U.P.)

Backend **.NET 8** + **MongoDB** + **JWT** para la plataforma multi-tenant del trabajo final.

## Estructura

```
TESIS/
├── MarketSaaS.sln
├── src/MarketSaaS.Api/    # API REST
├── INFORME/               # documentación / DER
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
| POST | `/api/negocios` | Crear negocio (slug único en la plataforma) |
| GET | `/api/negocios/{slug}` | Obtener negocio por slug |
| POST | `/api/auth/registro` | Registro (`SuperAdmin` sin `negocioId`; otros roles con `negocioId`) |
| POST | `/api/auth/login` | Login → JWT |
| GET | `/api/auth/me` | Perfil (requiere `Authorization: Bearer …`) |

Roles: `SuperAdmin`, `AdminTienda`, `Cliente`.

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

- Middleware de **resolución de tenant** por slug en ruta o header.
- CRUD **productos / variantes / stock** con `negocioId` en filtros.
- Políticas **SuperAdmin** vs **AdminTienda** en creación de negocios.
