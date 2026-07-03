# Agente Inteligente de Auditoría ISO 9001 — BDT Global

Trabajo final — Tecnicatura en Análisis de Sistemas (ORT).

Sistema que **audita automáticamente proyectos de desarrollo de software** de BDT Global contra su procedimiento de calidad (PR 11-13), combinando reglas determinísticas en C# con agentes LLM. Verifica que cada proyecto tenga la documentación exigida por su **tailoring**, que esa documentación exista realmente en Google Drive, que corresponda al proyecto y al formulario correctos, y que su contenido sea válido y vigente. Produce un resultado por artefacto (Conforme / No Conforme / No Aplica) con hallazgos clasificados (NC / OBS / OM) y un informe.

---

## Índice
- [Qué hace](#qué-hace)
- [Arquitectura](#arquitectura)
- [El workflow de auditoría (6 nodos)](#el-workflow-de-auditoría-6-nodos)
- [Stack tecnológico](#stack-tecnológico)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Requisitos previos](#requisitos-previos)
- [Configuración](#configuración)
- [Puesta en marcha](#puesta-en-marcha)
- [Cómo se ejecuta una auditoría](#cómo-se-ejecuta-una-auditoría)
- [API principal](#api-principal)
- [Tests](#tests)
- [Convenciones de arquitectura](#convenciones-de-arquitectura)
- [Notas de entrega y seguridad](#notas-de-entrega-y-seguridad)

---

## Qué hace

Una auditoría toma un **proyecto** y una **etapa** (Planificación, Desarrollo, etc.) y:

1. Arma el contexto desde la base (procedimiento, artefactos esperados, vigencias de Calidad).
2. Lee el **tailoring** del proyecto (formulario FR 29) desde Google Drive: qué artefactos aplican.
3. Busca físicamente cada evidencia en Drive / Trello / Clockify, la descarga y la analiza.
4. Verifica **existencia** (¿está la evidencia correcta?) y **contenido** (¿es válido y vigente?).
5. Clasifica los hallazgos en **NC** (No Conformidad), **OBS** (Observación) u **OM** (Oportunidad de Mejora).
6. Calcula el veredicto por artefacto, persiste el resultado y genera un informe.

El corazón del sistema son **agentes de IA (Gemini)** que interpretan documentos humanos y ambiguos —tailorings, formularios y planillas que cada proyecto llena distinto—; la lógica determinística que los rodea aporta verificación exacta y trazabilidad.

---

## Arquitectura

```
┌─────────────┐        HTTP (cookie JWT)        ┌──────────────────────────────┐
│  Frontend   │  ───────────────────────────▶   │        .NET Web API           │
│ React + Vite│  ◀───────────────────────────   │      (Controllers REST)       │
└─────────────┘                                 └──────────────┬───────────────┘
                                                               │ encola
                                                               ▼
                                                 ┌──────────────────────────────┐
                                                 │  AuditoriaWorkerService        │
                                                 │  (BackgroundService + cola)    │
                                                 └──────────────┬───────────────┘
                                                                │ ejecuta
                                                                ▼
                                    ┌───────────────────────────────────────────────┐
                                    │      Workflow MAF de 6 nodos (orquestador)     │
                                    │  Contexto → Análisis → [Compliance ∥ Consist.] │
                                    │            → Clasificación → Consolidador       │
                                    └───────┬───────────────┬───────────────┬────────┘
                                            │               │               │
                                     ┌──────▼─────┐  ┌───────▼──────┐  ┌─────▼──────┐
                                     │  Gemini    │  │  MCP servers  │  │   MySQL    │
                                     │ (IChatClient)│ │ Drive/Trello/ │  │ (EF Core)  │
                                     └────────────┘  │  Clockify     │  └────────────┘
                                                     └───────────────┘
```

- **Frontend** (React + Vite): pantallas de proyectos, auditorías, hallazgos, informes, dashboard y configuración. Se autentica con un **JWT en cookie HttpOnly**.
- **API REST** (Controllers): recibe el pedido de auditoría, lo **encola** y responde `201 Created`. La auditoría corre en background.
- **Worker + Runner**: toman la auditoría de la cola, crean un scope de DI propio, arman los 6 nodos y ejecutan el **workflow de Microsoft Agent Framework (MAF)**. Al terminar, persisten el resultado y generan el informe. Trackean progreso por nodo y consumo de tokens.
- **MCP in-process**: los servidores de Google Drive, Trello y Clockify se montan dentro de la misma app (`/mcp/drive`, `/mcp/trello`, `/mcp/clockify`) y centralizan el acceso a esas herramientas.
- **Gemini** detrás de `IChatClient`: los 4 agentes LLM del workflow.
- **MySQL** vía EF Core (Pomelo): proyectos, procedimientos, auditorías, hallazgos, informes, etc.

---

## El workflow de auditoría (6 nodos)

El **orden es fijo, definido en código** — no lo decide la IA. En **4 de los 6 nodos**, agentes LLM (Gemini) realizan la interpretación experta que ningún criterio fijo puede resolver; a su alrededor, lógica determinística aporta verificación exacta y trazabilidad.

| # | Nodo | Tipo | Qué hace |
|---|------|------|----------|
| 1 | **Contexto** (`ResolutorContexto`) | Determinístico | Lee la BD y precalcula, por artefacto: exigibilidad, obligatoriedad (tipo A/B), template, vigencia esperada y cuál es el tailoring. Falla temprano si el input no es coherente. |
| 2 | **Análisis Documental** | 🧠 IA + I/O | Baja el tailoring de Drive; el LLM cruza los artefactos esperados contra el tailoring (aplica / no aplica / no declarado). Luego busca la evidencia físicamente, la hashea, parsea secciones y extrae metadata (vigencia, código, proyecto). **El documento se lee una sola vez, acá.** |
| 3 | **Compliance** | 🧠 IA | *¿Está la evidencia correcta?* El LLM interpreta la coherencia entre la URL declarada en el tailoring y el archivo hallado; en apoyo, reglas determinísticas cubren los casos objetivos (falta, no-aplica sin justificar, no declarado, documento de otro proyecto/formulario, responsable vacío). Corre en **paralelo** con el nodo 4. |
| 4 | **Consistencia** | 🧠 IA | *¿El contenido está bien?* El LLM evalúa, según el **propósito** del documento, si una sección vacía es realmente un problema —un juicio que exige entender para qué sirve cada artefacto—. En apoyo, reglas verifican la vigencia del formulario y las secciones ausentes vs template. |
| 5 | **Clasificación** | 🧠 IA | El LLM **califica la severidad** de cada hallazgo (NC / OBS / OM) según su contexto. Para los casos de criticidad inequívoca, pisos por regla garantizan consistencia (identidad → NC; vigencia/responsable → OBS; tailoring → tope OM). |
| 6 | **Consolidador** | Determinístico | Calcula el veredicto por artefacto con reglas puras, arma hallazgos y documentos analizados, y persiste. |

Los documentos extraídos en el nodo 2 viajan por un **carril directo** hasta el 5 (para clasificar con contexto) y de ahí al 6 (para el veredicto final).

---

## Stack tecnológico

**Backend**
- .NET 9 · ASP.NET Core Web API (Controllers)
- Entity Framework Core + Pomelo MySQL
- Microsoft Agent Framework (`Microsoft.Agents.AI` / `.Workflows`)
- Model Context Protocol (`ModelContextProtocol` + `.AspNetCore`)
- Gemini vía `Microsoft.Extensions.AI` (`Mscc.GenerativeAI.Microsoft`)
- Google Drive API (`Google.Apis.Drive.v3`)
- Parsers: ClosedXML (xlsx), DocumentFormat.OpenXml (docx), PdfPig (pdf)
- BCrypt (hash de contraseñas) + JWT
- Tests: xUnit + golden snapshots

**Frontend**
- React 19 + TypeScript + Vite 6
- react-router-dom 7
- jspdf + html2canvas (export de informes a PDF)

**Base de datos**
- MySQL 8.x

---

## Estructura del repositorio

```
proyecto_final/
├── backend/
│   ├── ISOAuditAgent.API.sln
│   ├── seed-demo.sql                  # Seed idempotente (marco + demo)
│   ├── ISOAuditAgent.API/
│   │   ├── Program.cs                 # Composición: DI, JWT, CORS, MCP, worker, seed
│   │   ├── Controllers/               # Auth, Proyecto, Auditoria, Procedimiento,
│   │   │                              #   Dashboard, Hallazgo, Informe
│   │   ├── Agents/
│   │   │   ├── Contracts/             # DTOs que viajan entre nodos del workflow
│   │   │   ├── DocumentAnalysis/      # Tailoring, Drive, parsing, armado de artefactos
│   │   │   ├── ComplianceValidation/  # ¿existe la evidencia correcta?
│   │   │   ├── ConsistencyVerification/ # ¿el contenido es correcto?
│   │   │   ├── FindingsClassification/  # NC / OBS / OM
│   │   │   └── Orchestrator/          # Workflow MAF
│   │   │       ├── Execution/         # Cola, worker, runner, consumo de tokens
│   │   │       ├── Nodes/             # Los 6 nodos
│   │   │       └── Workflow/          # Factory del grafo + ensamblador final
│   │   ├── Integrations/
│   │   │   ├── LLM/                   # Gemini + wiring de los AIAgent
│   │   │   └── MCP/{Drive,Trello,Clockify}/
│   │   ├── Data/                      # DbContext + DataSeeder
│   │   ├── Models/                    # Entidades y enums EF
│   │   ├── Repositories/              # Repositorios + UnitOfWork
│   │   ├── Services/                  # Servicios de aplicación
│   │   ├── DTOs/
│   │   ├── Migrations/                # Migraciones EF Core
│   │   └── secrets/                   # Service account de Drive (gitignored)
│   └── ISOAuditAgent.API.Tests/       # xUnit + golden snapshots
└── frontend/
    ├── package.json
    ├── vite.config.ts                 # Proxy /api → backend :5180
    └── src/
        ├── api/                       # Cliente fetch + endpoints tipados
        ├── screens/                   # Login, Dashboard, Proyectos, Hallazgos,
        │                              #   Informes, Usuarios, Configuración
        ├── components/
        └── login/                     # Auth context + rutas protegidas
```

---

## Requisitos previos

- **.NET 9 SDK**
- **MySQL 8.x** local (o accesible)
- **Node.js 18+** (para el frontend)
- **`dotnet-ef`** (herramienta de migraciones): `dotnet tool install --global dotnet-ef`
- **Service account de Google Drive** con acceso a las carpetas de los proyectos
- **API key de Gemini**

---

## Configuración

Los secretos **no se commitean**: van en User Secrets (backend) y en el archivo de la service account. `appsettings.Development.json` es solo para desarrollo local (gitignored).

Como referencia de la estructura completa hay un **`appsettings.Development.example.json`** commiteado (sin claves): copialo a `appsettings.Development.json` y completá los valores, o cargá los secretos con User Secrets.

Desde `backend/ISOAuditAgent.API`:

```bash
# Base de datos
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Port=3306;Database=iso_audit_agent;User=root;Password=TU_PASSWORD"

# Gemini
dotnet user-secrets set "Gemini:ApiKey" "TU_API_KEY"
dotnet user-secrets set "Gemini:ModelId" "gemini-2.5-flash"   # o el modelo que uses

# JWT (clave larga y secreta)
dotnet user-secrets set "Jwt:SecretKey" "UNA_CLAVE_LARGA_Y_SECRETA"
dotnet user-secrets set "Jwt:Issuer" "ISOAuditAgent"
dotnet user-secrets set "Jwt:Audience" "ISOAuditAgentFrontend"

# Trello / Clockify (si se auditan esas fuentes)
dotnet user-secrets set "Trello:ApiKey" "..."
dotnet user-secrets set "Trello:Token" "..."
dotnet user-secrets set "Clockify:ApiKey" "..."
dotnet user-secrets set "Clockify:WorkspaceId" "..."
```

**Google Drive:** el backend lee los archivos con una **service account**. Para obtenerla:

1. Crear una service account en Google Cloud Console y habilitar la **Google Drive API**.
2. Descargar su clave en formato JSON.
3. **Compartir las carpetas de Drive de los proyectos con el email de la service account** (permiso de lectura), para que pueda listarlas y descargar los archivos.

Dejar el JSON en:

```
backend/ISOAuditAgent.API/secrets/google-service-account.json
```

La ruta se configura en `GoogleDrive:ServiceAccountKeyPath`. La carpeta `secrets/` está gitignored — **nunca commitear el JSON real** (no se versiona ninguna plantilla: es una credencial que se descarga, no se completa a mano).

Claves de configuración que usa la app:

| Clave | Para qué |
|-------|----------|
| `ConnectionStrings:DefaultConnection` | Conexión a MySQL |
| `Gemini:ApiKey` / `Gemini:ModelId` | Modelo LLM |
| `Jwt:SecretKey` / `Issuer` / `Audience` | Firma y validación del token |
| `GoogleDrive:ServiceAccountKeyPath` | Credencial de Drive |
| `Trello:*` / `Clockify:*` | Evidencias en esas herramientas |

---

## Puesta en marcha

### Backend

```bash
cd backend/ISOAuditAgent.API

dotnet restore
dotnet ef database update       # crea/actualiza la base y aplica migraciones
dotnet run                      # levanta la API (por defecto en http://localhost:5180)
```

En **desarrollo**, al arrancar se aplican las migraciones y se siembran 3 usuarios demo (`admin@bdtglobal.com.ar`, `auditor@…`, `operador@…`, contraseña `admin1234`). En **producción** esto no corre: hay que crear el primer usuario y cargar la base aparte.

**Seed de datos.** Hay dos, ambos idempotentes; ejecutar el que corresponda sobre la base `iso_audit_agent` (por ejemplo desde MySQL Workbench):

- **Instalación (requerido):** [`backend/seed-install.sql`](backend/seed-install.sql) — carga el **marco**: procedimiento **PR 11-13**, sus 5 etapas, los 18 artefactos esperados, cuál es el tailoring y la referencia de vigencias de Calidad. ⚠️ Antes de correrlo, **reemplazar el placeholder del folder de templates** por la carpeta de Drive de BDT. No carga proyectos ni usuarios.
- **Demo (solo desarrollo):** [`backend/seed-demo.sql`](backend/seed-demo.sql) — el marco + un proyecto de prueba (App Productores) apuntando a una carpeta de Drive de ejemplo, para probar el sistema end-to-end.

### Frontend

```bash
cd frontend

npm install
npm run dev                     # levanta Vite en http://localhost:5173
```

El dev server de Vite hace **proxy de `/api` al backend en `localhost:5180`** (ver `vite.config.ts`), así que alcanza con tener el backend corriendo. Entrar a `http://localhost:5173` y loguearse con un usuario demo.

---

## Cómo se ejecuta una auditoría

1. El usuario dispara una auditoría desde el front (o `POST /api/auditorias`).
2. La API crea la auditoría en estado **EnCurso**, la **encola** y responde `201 Created`.
3. El `AuditoriaWorkerService` (background) la toma de la cola y el `AuditoriaRunner`:
   - crea un scope de DI propio,
   - arma los 6 nodos y el workflow MAF,
   - lo ejecuta consumiendo el stream de eventos,
   - persiste el `AuditoriaResultado` (marca **Completada**),
   - genera el informe automático.
4. Cualquier error se traduce a estado **Fallida** con categoría y mensaje legible; el worker sigue con la siguiente auditoría.
5. El front hace polling del **progreso por nodo** y, al terminar, muestra el resultado y los hallazgos.

---

## API principal

Todos los endpoints (salvo `login` / `logout`) requieren estar autenticado; el JWT viaja en la cookie `auth_token`. Roles: **Administrador**, **Auditor**, **Operador**.

**Auth y usuarios** (`/api/auth`)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/auth/login` | Login (público) → cookie JWT |
| `POST` | `/api/auth/logout` | Cerrar sesión |
| `GET` | `/api/auth/me` | Perfil del usuario logueado |
| `PUT` | `/api/auth/me/tema` · `/me/password` | Cambiar tema / propia contraseña |
| `GET`·`POST`·`PUT` | `/api/auth/usuarios[...]` | Gestión de usuarios (Admin) |

**Proyectos** (`/api/proyectos`)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/proyectos` · `/{id}` | Listar (según rol) / detalle |
| `POST`·`PUT` | `/api/proyectos` · `/{id}` | Crear / modificar (Admin) |
| `POST`·`DELETE` | `/api/proyectos/{id}/responsables[...]` | Asignar / quitar responsable (Admin) |

**Auditorías** (`/api/auditorias`)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/auditorias` | Disparar auditoría → **`201 Created`** (Admin/Auditor) |
| `GET` | `/api/auditorias` | Todas (Admin) |
| `GET` | `/api/auditorias/proyecto/{proyectoId}` | De un proyecto |
| `GET` | `/api/auditorias/{id}` | Estado |
| `GET` | `/api/auditorias/{id}/resultado` | Resultado completo |
| `GET` | `/api/auditorias/{id}/progreso` | Progreso por nodo (polling del front) |
| `GET` | `/api/auditorias/{id}/errores` | Errores registrados |
| `PUT` | `/api/auditorias/{id}/estado` | Cambiar estado (Admin) |

**Procedimientos** (`/api/procedimientos`)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/procedimientos` · `/{id}` | Listar / detalle |
| `GET` | `/api/procedimientos/{id}/etapas` | Etapas de un procedimiento |
| `GET` | `/api/procedimientos/etapas/{etapaId}/artefactos` | Artefactos de una etapa |
| `POST`·`PUT` | `/api/procedimientos/artefactos[...]` | Crear / modificar artefacto (Admin) |

**Hallazgos** (`/api/hallazgos`)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/hallazgos/revision?scope=actual\|historico` | Revisión consolidada (por rol) |
| `GET` | `/api/hallazgos/auditoria/{auditoriaId}` · `/{id}` | De una auditoría / detalle |

**Informes** (`/api/informes`)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/informes` | Todos (Admin/Auditor) |
| `GET` | `/api/informes/auditoria/{auditoriaId}` · `/{id}` | De una auditoría / detalle |
| `POST` | `/api/informes/manual` | Generar informe manual |
| `POST` | `/api/informes/automatico/{auditoriaId}` | Generar informe automático (Admin) |

**Dashboard** (`/api/dashboard`)

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/dashboard` | Métricas agregadas (Admin/Auditor) |

---

## Tests

```bash
cd backend
dotnet test
```

Los tests usan **xUnit** y **golden snapshots** (`Snapshot.MatchJson`). En la primera corrida de un caso nuevo se escribe un `.received.txt` y el test falla; se aprueba copiándolo a `.approved.txt` y se vuelve a correr. Cubren los generadores determinísticos (hallazgos, matchers), los parsers y los prompt builders.

> Nota: el `.exe` de la API se bloquea si la app está corriendo. Frená la app antes de correr los tests si aparece un error de copia (`MSB3021`).

---

## Convenciones de arquitectura

- **Endpoints**: Controllers (`[ApiController]` + `ControllerBase`), no Minimal API.
- **DI**: registrado en extensiones por dominio, no uno por uno en `Program.cs`.
- **Workflow**: 6 nodos MAF; no se agregan nodos sin discutir.
- **Tipos**: entidades EF (`class` con `{ get; set; }`) en `Models/`; contratos/DTOs (`sealed record` con `IReadOnlyList<T>`) en `Agents/Contracts/`; enums persistidos como **texto**.
- **Lifetimes**: repositorios y nodos → Scoped; clientes externos (HTTP/MCP/LLM), AIAgents, cola y runner → Singleton; workers → HostedService.
- **Persistencia**: tablas en plural snake_case; FK con sufijo `_id`; cambios de schema **siempre por migración EF**.
- **MCP**: servidores in-process montados en `/mcp/<servicio>`.
- **LLM**: `IChatClient` como abstracción, Gemini detrás; extractor de JSON tolerante a prosa.
- **Idioma**: dominio y comentarios en español; términos técnicos universales en inglés.

---

## Notas de entrega y seguridad

- Los secretos de desarrollo viven en `appsettings.Development.json`, que **está gitignored** (no se commitea) — igual que la carpeta `secrets/`. El único `appsettings.json` trackeado va sin claves.
- Antes de publicar, confirmá que ningún secreto quedó trackeado: `git ls-files | grep -iE "appsettings.Development|secret"` (no debería devolver nada sensible).
- **No commitear** `secrets/google-service-account.json`, `bin/`, `obj/`, `.vs/`.
- En **producción** el sembrado automático de usuarios demo no corre: definir cómo se crea el primer administrador y cargar el marco (procedimiento + artefactos) con el seed.
- El **marco** (procedimiento, etapas, artefactos, vigencias) es dato de BDT: se entrega cargado por el seed; el cliente carga sus **proyectos** desde el front.
