# Agente ISO 9001 — Frontend

Frontend de la plataforma de auditoría ISO 9001 de **BDT Global**. Permite a los auditores ejecutar workflows de auditoría asistida por IA, revisar hallazgos, generar informes y administrar su cuenta.

---

## Stack

| Capa | Tecnología | Versión |
|---|---|---|
| Framework | React | 19.0.0 |
| Lenguaje | TypeScript (estricto) | 5.7.2 |
| Build / dev server | Vite | 6.0.0 |
| Router | React Router DOM | 7.1.5 |
| Generación de PDF | jsPDF | (instalar con `npm install jspdf`) |
| Estilos | CSS-in-string + `useInjectStyle` | propio |
| Auth | JWT en `sessionStorage` + header `Authorization: Bearer` | propio |
| Backend | .NET 9 + MySQL + EF Core | corre en `localhost:5180` |

Sin librerías UI externas (ni Material UI, ni Tailwind, ni shadcn). Todo el diseño es propio para mantener consistencia y control.

---

## Cómo arrancarlo

### Requisitos previos

- **Node.js** 20+ y **npm**
- **.NET SDK 9** (para el backend)
- **MySQL** corriendo en `localhost:3306` con la BD `iso_audit_agent` creada
- **Un usuario admin** insertado en la tabla `usuarios` (ver `sql/primer_usuario.sql` del repo)
- **Variables de entorno del backend** configuradas (`Jwt:SecretKey` como user-secret, connection string a MySQL)

### Primera vez

```bash
# Clonar y entrar al frontend
cd Agente-ISO-9001/frontend

# Instalar dependencias
npm install

# Instalar jsPDF (necesario para el export en /hallazgos)
npm install jspdf
```

### Levantar en desarrollo

Necesitás **dos terminales**:

**Terminal 1 — Backend** (en `backend/ISOAuditAgent.API/`):
```bash
dotnet run
# Backend escuchando en http://localhost:5180
```

**Terminal 2 — Frontend** (en `frontend/`):
```bash
npm run dev
# Frontend en http://localhost:5173
```

Abrí `http://localhost:5173` en el browser. Te redirige automáticamente a `/login`.

**Credenciales del admin** (creado por el script `primer_usuario.sql`):
- Email: `admin@bdtglobal.com.ar`
- Contraseña: `Admin1234!`

### Scripts disponibles

```bash
npm run dev      # Dev server con hot reload (puerto 5173)
npm run build    # Compila TypeScript + bundle de producción → dist/
npm run preview  # Sirve el build de producción para probarlo localmente
npm run lint     # Verifica solo tipos con tsc --noEmit (no compila)
```

---

## Estructura del proyecto

```
frontend/
├── index.html                    # Entry HTML, monta <div id="root">
├── package.json                  # Dependencias y scripts npm
├── tsconfig.json                 # Config TypeScript (strict: true)
├── vite.config.ts                # Config Vite (proxy /api → :5180)
└── src/
    ├── main.tsx                  # Entry point — monta <App> en #root
    ├── App.tsx                   # Router principal + AuthProvider
    │
    ├── api/                      # Clientes HTTP (1 archivo por dominio)
    │   ├── client.ts             # Wrapper sobre fetch + JWT automático
    │   ├── auditorias.ts         # POST /api/auditorias, GET .../{id}, etc.
    │   ├── procedimientos.ts     # GET /api/procedimientos/{id}/etapas
    │   ├── proyectos.ts          # GET /api/proyectos
    │   └── hallazgos.ts          # Tipos + mock data (28 hallazgos)
    │
    ├── components/               # Componentes reusables
    │   ├── Sidebar.tsx           # Navegación lateral con datos del JWT
    │   ├── EjecucionAuditoria.tsx# Monitor del workflow en curso
    │   └── HallazgoDetalleModal.tsx # Modal del detalle del hallazgo
    │
    ├── screens/                  # Pantallas completas (1 por ruta)
    │   ├── NuevaAuditoria.tsx    # /nueva-auditoria
    │   ├── Hallazgos.tsx         # /hallazgos
    │   └── Configuracion.tsx     # /configuracion
    │
    ├── login/                    # Todo lo de autenticación juntito
    │   ├── Login.tsx             # /login — split-screen con animación
    │   ├── AuthContext.tsx       # Estado global de sesión
    │   ├── ProtectedRoute.tsx    # HOC que redirige a /login si no hay sesión
    │   ├── authApi.ts            # POST /api/auth/login, GET /api/auth/me
    │   ├── NetworkBackground.tsx # Canvas del fondo animado
    │   ├── InputField.tsx        # Input con label flotante + toggle ojo
    │   └── loginStyles.ts        # CSS-in-string del login (tokens incluidos)
    │
    ├── styles/                   # Estilos compartidos
    │   ├── shared.ts             # CSS del shell (sidebar + main)
    │   ├── hallazgos.ts          # CSS de la pantalla Hallazgos
    │   └── configuracion.ts      # CSS de la pantalla Configuración
    │
    └── utils/
        ├── useInjectStyle.ts     # Hook para inyectar CSS-in-string una vez
        └── exportHallazgosPdf.ts # Generador de PDF con jsPDF
```

---

## Rutas

| Ruta | Acceso | Componente | Descripción |
|---|---|---|---|
| `/login` | 🔓 Pública | `<Login />` | Pantalla de inicio de sesión con animación split-screen |
| `/nueva-auditoria` | 🔒 Protegida | `<NuevaAuditoria />` | Crear auditorías nuevas y ver el workflow en curso |
| `/hallazgos` | 🔒 Protegida | `<Hallazgos />` | Tabla de hallazgos con filtros, modal y export PDF |
| `/configuracion` | 🔒 Protegida | `<Configuracion />` | 4 tabs: perfil, notificaciones, integraciones, agente IA |
| `*` (cualquier otra) | — | `<Navigate />` | Redirige a `/nueva-auditoria` |

Las rutas protegidas están envueltas en `<ProtectedRoute>` que verifica `estaAutenticado` del `AuthContext`. Si no hay sesión, redirige a `/login` guardando la ruta original en el `state` para volver después.

---

## Autenticación y JWT

### Flujo completo

```
1. Usuario tipea email + password en /login
        ↓
2. Login.tsx llama a iniciarSesion() del AuthContext
        ↓
3. AuthContext llama a authApi.login()
        ↓
4. authApi.login() hace POST /api/auth/login
        ↓
5. Backend valida (dominio @bdtglobal.com.ar + BCrypt + usuario activo)
        ↓
6. Backend devuelve { token, nombre, email, rol, expiracion }
        ↓
7. AuthContext guarda en sessionStorage:
   - "token" → string del JWT
   - "usuario" → JSON con { nombre, email, rol, expiracion }
        ↓
8. estaAutenticado pasa a true → ProtectedRoute deja pasar
        ↓
9. Cada request siguiente (api.get / api.post) agrega el header
   Authorization: Bearer <token> automáticamente
        ↓
10. Si el backend devuelve 401, client.ts limpia sessionStorage
    y redirige a /login (token vencido o inválido)
```

### Validación de dominio

Solo cuentas con email terminando en `@bdtglobal.com.ar` o `@bdtglobal.com` pueden loguearse. Esto se valida:

- **En el frontend** (`Login.tsx`) para UX rápida — muestra el error sin llamar al backend
- **En el backend** (`AuthService.LoginAsync`) como validación real — un atacante no puede saltarla con curl

### Identificación del usuario en cada request

El JWT contiene en sus `claims`:

| Claim | Contenido |
|---|---|
| `NameIdentifier` | ID numérico del usuario |
| `Name` | Nombre completo |
| `Email` | Email |
| `Role` | `Administrador` / `Auditor` / `Operador` |

El backend lee `NameIdentifier` con `ClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)` para saber **quién** está haciendo cada request. Por eso `POST /api/auditorias` ya no recibe `usuarioId` en el body — sale del token.

### Persistencia

Usamos `sessionStorage` (no `localStorage`) por seguridad. Cuando se cierra el browser, la sesión se pierde. Si querés persistencia entre cierres, se cambia en `AuthContext.tsx`.

### Logout

El botón `⎋` del sidebar llama a `cerrarSesion()`, que:
1. Borra `token` y `usuario` de `sessionStorage`
2. Pone `usuario` a `null` en el state
3. Redirige a `/login`

---

## Convenciones de código

### TypeScript estricto

`tsconfig.json` tiene `"strict": true` y `"noUnusedParameters": true`. Reglas:

- **Sin `any`** — usar tipos específicos o `unknown` cuando sea necesario
- **Sin parámetros no usados** — si no se usa, sacarlo
- **Tipar siempre los props** con `interface` antes del componente
- **Records inmutables** para DTOs (en backend) y `interface` para tipos del frontend
- **Lo del backend está en español** (Auditoria, Hallazgo, etc.) y el frontend respeta la misma terminología

### CSS-in-string

Patrón propio para evitar configurar CSS Modules o frameworks. Funciona así:

**1. Definir el CSS como string en `src/styles/[pantalla].ts`:**

```ts
// src/styles/miPantalla.ts
export const miPantallaCss = `
.mp-card {
  background: #fff;
  border-radius: 10px;
  padding: 1rem;
}
`;
```

**2. Inyectarlo en el componente con `useInjectStyle`:**

```tsx
import { useInjectStyle } from '../utils/useInjectStyle';
import { miPantallaCss } from '../styles/miPantalla';

export default function MiPantalla() {
  useInjectStyle(miPantallaCss, 'mi-pantalla-style');
  return <div className="mp-card">…</div>;
}
```

**3. El hook agrega un `<style id="mi-pantalla-style">` al `<head>` una sola vez.** En remounts no se duplica gracias al `id`.

**Convención de naming:** prefijo de 2-3 letras por pantalla para evitar colisiones:
- `hz-` → Hallazgos
- `cfg-` → Configuración
- `login-` → Login
- `sb-`, `nav-`, etc. → Compartidos (`shared.ts`)

### Paleta de colores

| Variable conceptual | Hex | Uso |
|---|---|---|
| Brand primary | `#4B2DAB` | Botones, badges activos, links |
| Brand dark | `#1e1050` | Texto principal, headings |
| Brand light bg | `#f0eefa` | Background del body |
| Card bg | `#faf8ff` | Background de cards/inputs |
| Brand accent | `#7B52E8` | Focus states, hover acento |
| Text muted | `#7b6aaa` | Subtítulos, labels |
| Text very muted | `#9080bb` | Placeholders, iconos secundarios |
| Border | `rgba(120,80,200,0.15-0.25)` | Bordes sutiles de cards |
| Error | `#A32D2D` (texto), `#FCEBEB` (bg) | Errores, no conformidades |
| Warning | `#BA7517` (texto), `#FFF4E5` (bg) | Observaciones, "en desarrollo" |
| Success | `#27500A` (texto), `#EAF3DE` (bg) | Resueltos, conectados |

### API modules

Cada dominio tiene su archivo en `src/api/`. Estructura típica:

```ts
// src/api/algo.ts
import { api } from './client';

// 1. Tipos públicos
export interface Algo {
  id: number;
  nombre: string;
}

// 2. Helpers de presentación (opcional)
export const TIPO_LABEL: Record<TipoAlgo, string> = { … };

// 3. Funciones que llaman al backend
export function listar(): Promise<Algo[]> {
  return api.get<Algo[]>('/api/algo');
}

export function crear(req: CrearAlgoRequest): Promise<Algo> {
  return api.post<Algo>('/api/algo', req);
}
```

El cliente `api` está en `src/api/client.ts` y maneja:
- Header `Content-Type: application/json` automático
- Header `Authorization: Bearer <token>` automático (si hay token en `sessionStorage`)
- Parsing de errores con el body de la respuesta
- Redirección a `/login` si el backend devuelve 401

### Estilo de commits

Usamos prefijos tipo Conventional Commits, aunque sin obsesión:

```
feat(auth): login JWT con validación de dominio @bdtglobal.com.ar
feat(hallazgos): pantalla con filtros, modal y export PDF
feat(configuracion): pantalla con 4 tabs
fix(sidebar): mostrar nombre del usuario logueado
refactor(api): extraer DEFAULT_USUARIO_ID al usar JWT
```

---

## Pantallas en detalle

### Login (`/login`)

**Archivo:** `src/login/Login.tsx`

Pantalla con split-screen animado entre dos vistas (Sign In / Sign Up).

- **Lado izquierdo (blanco):** formulario de email + password
- **Lado derecho (violeta gradiente):** branding con logo BDT + estadística "100% Trazable"
- **Fondo:** canvas con red de nodos animados (`NetworkBackground.tsx`)

**Características:**
- Validación de dominio antes de llamar al backend
- Toggle ojo para mostrar/ocultar contraseña
- Spinner durante el loading
- Error visible si la validación falla o el backend rechaza
- Sign Up muestra "Funcionalidad en desarrollo" (placeholder)

### Nueva auditoría (`/nueva-auditoria`)

**Archivo:** `src/screens/NuevaAuditoria.tsx`

Pantalla principal del flujo de creación de auditorías.

- Selector de proyecto (autocomplete con datos del backend)
- Selector de etapa del procedimiento
- Botón "Iniciar auditoría" → llama a `POST /api/auditorias`
- Una vez iniciada, muestra `<EjecucionAuditoria>` con progreso por nodo

### Hallazgos (`/hallazgos`)

**Archivo:** `src/screens/Hallazgos.tsx`

Tabla completa de hallazgos detectados por los agentes IA.

**Features:**
- **4 tarjetas resumen** (calculadas dinámicamente con `useMemo`):
  - No conformidades (rojo)
  - Observaciones (naranja)
  - Oportunidades de mejora (verde)
  - Total hallazgos
- **Buscador por texto** (filtra título, descripción, proyecto, evidencia)
- **5 filtros pill** (Todos / NC / OBS / OM / Sin resolver)
- **Tabla** con badges de colores, paginación de 6 por página
- **Modal de detalle** al click "Ver →" (cierre con ESC, click fuera, o ×)
- **Export a PDF** con `jsPDF` — exporta SOLO los hallazgos filtrados

**⚠️ Importante:** los datos son **mock** (28 hallazgos hardcodeados en `src/api/hallazgos.ts`). Cuando el backend exponga `GET /api/hallazgos`, solo hay que cambiar la función `listarHallazgos()`:

```ts
// Antes (mock):
export function listarHallazgos(): Promise<Hallazgo[]> {
  return new Promise((resolve) => setTimeout(() => resolve(MOCK), 300));
}

// Después (real):
export function listarHallazgos(): Promise<Hallazgo[]> {
  return api.get<Hallazgo[]>('/api/hallazgos');
}
```

El JWT se manda automáticamente. La interface `Hallazgo` define la forma esperada del response.

### Configuración (`/configuracion`)

**Archivo:** `src/screens/Configuracion.tsx`

Pantalla con 4 tabs:

#### Tab 1 — Mi perfil
- Datos del usuario logueado en **solo lectura** (Nombre, Email, Rol) — extraídos del `AuthContext`
- Card informativa sobre contraseña — explica que por seguridad no se puede ver, que contacte al administrador
- **No hay formulario de cambio de contraseña** todavía (se agregará cuando exista el endpoint en el backend)

#### Tab 2 — Notificaciones
- 4 items de notificaciones por email + 2 del sistema
- Cada uno tiene un badge naranja **"Función en desarrollo"** (no son toggles funcionales)

#### Tab 3 — Integraciones
- Banner azul: **"Vista de ejemplo — el monitoreo en vivo se incorporará próximamente"**
- Mock con Google Drive (✓), Trello (✓) y Clockify (✕)
- **Los datos son ilustrativos** — no consulta el backend

#### Tab 4 — Agente IA
- Banner azul: **"Vista de ejemplo — el monitoreo en vivo se incorporará próximamente"**
- Modelo actual: Gemini 2.5 Flash (mock con badge "Configurable en desarrollo")
- 4 opciones de clasificación con badge "Función en desarrollo"

---

## Conexión con el backend

### Configuración del proxy

`vite.config.ts` redirige todas las llamadas a `/api/*` al backend en `localhost:5180`:

```ts
server: {
  port: 5173,
  proxy: {
    '/api': {
      target: 'http://localhost:5180',
      changeOrigin: true,
      secure: false,
    },
  },
}
```

Esto evita problemas de CORS en desarrollo. En producción, el frontend y backend pueden vivir en el mismo dominio (o el backend tiene CORS habilitado para el dominio del frontend).

### Endpoints consumidos

| Método | Endpoint | Auth | Frontend |
|---|---|---|---|
| `POST` | `/api/auth/login` | 🔓 | `authApi.login()` |
| `GET` | `/api/auth/me` | 🔒 | `authApi.obtenerPerfil()` |
| `GET` | `/api/proyectos` | 🔒 | `proyectosApi.listar()` |
| `GET` | `/api/procedimientos/{id}/etapas` | 🔒 | `procedimientosApi.listarEtapas()` |
| `POST` | `/api/auditorias` | 🔒 | `auditoriasApi.crear()` |
| `GET` | `/api/auditorias/{id}` | 🔒 | `auditoriasApi.obtener()` |
| `GET` | `/api/auditorias/{id}/progreso` | 🔒 | `auditoriasApi.obtenerProgreso()` |

🔒 = requiere JWT en header `Authorization: Bearer <token>`

### Endpoints aún no implementados (mocks en frontend)

| Endpoint esperado | Mock actual en |
|---|---|
| `GET /api/hallazgos` | `src/api/hallazgos.ts` (28 hallazgos hardcoded) |
| `GET /api/configuracion/agente` | `src/screens/Configuracion.tsx` (modelo IA hardcoded) |
| `GET /api/integraciones/estado` | `src/screens/Configuracion.tsx` (Drive/Trello/Clockify hardcoded) |
| `POST /api/auth/cambiar-password` | No implementado (card informativa en su lugar) |

---

## Decisiones técnicas importantes

### Por qué TypeScript estricto

Detectar errores en compile-time en lugar de runtime. Con `strict: true`:
- `null`/`undefined` no se confunden con valores válidos
- Funciones sin retorno explícito son detectadas
- Variables no inicializadas son flagged
- Tipos implícitos (`any`) están prohibidos

Costo: hay que tipar más. Beneficio: menos bugs en producción y autocomplete confiable en el editor.

### Por qué no Tailwind / Material UI / etc.

Decisión consciente para mantener control total del diseño y evitar añadir 50KB+ de CSS no usado. La paleta es propia (morada), el diseño es propio, y el patrón `useInjectStyle` es suficiente.

Si en algún momento crece la complejidad, se puede migrar a CSS Modules sin mucho trabajo (cambiar `import { css } from '../styles/x'` por `import styles from '../styles/x.module.css'`).

### Por qué CSS-in-string y no styled-components

- **No agrega runtime overhead** — los estilos se inyectan una sola vez al montar
- **No necesita configuración de build** — funciona out of the box con Vite
- **Compatible con TypeScript estricto** — el CSS es un string, no tiene tipos que validar
- **Fácil de migrar** — si querés pasar a CSS Modules, solo copiás el contenido a un `.module.css`

### Por qué sessionStorage y no localStorage para el token

`sessionStorage` se borra al cerrar el browser. Más seguro que `localStorage`, que persiste indefinidamente y es vulnerable si alguien accede a la máquina.

Trade-off: si querés que el usuario no tenga que loguearse cada vez que cierra y abre el browser, podés cambiar a `localStorage`. Pero perderías esa capa extra de protección.

### Por qué redirección automática en 401

El `client.ts` intercepta cualquier 401 y manda al usuario a `/login`. Razones:

- **Token vencido:** después de 8 horas el JWT expira y todos los endpoints empiezan a fallar
- **Token modificado:** si alguien tocó el `sessionStorage` a mano, el backend lo rechaza
- **Usuario desactivado:** si el admin desactivó al usuario, los tokens existentes siguen siendo válidos hasta vencer, pero el sistema los rechaza

En todos esos casos, mejor mandar al login que mostrar errores random.

### Por qué los mocks de Hallazgos / Configuración

Para poder mostrar las pantallas **funcionando visualmente** sin esperar a que el backend tenga esos endpoints. Cuando estén, son cambios de **una función** cada uno.

**Lo que SÍ está conectado al backend:**
- Login (real)
- Datos del usuario logueado en sidebar y configuración (vienen del JWT)
- Crear auditoría
- Listar proyectos y etapas
- Progreso del workflow

**Lo que fakta conectar:**
- Lista de hallazgos
- Modelo del agente IA configurado
- Estado de las integraciones (Drive/Trello/Clockify)
- Cambio de contraseña

---

## Próximos pasos 

### Backend pendiente

- `GET /api/hallazgos` — lista de hallazgos del usuario logueado o de todo el sistema según rol
- `POST /api/auth/cambiar-password` — endpoint para cambio de contraseña con validación de password actual
- `GET /api/configuracion/agente` — devuelve modelo IA + idioma + parámetros (lee `appsettings.json`)
- `GET /api/integraciones/estado` — verifica conexión real con Drive/Trello/Clockify

### Frontend pendiente

- Dashboard `/dashboard` — gráficos de tendencias, hallazgos por proyecto, KPIs
- Lista de proyectos `/proyectos` con detalle individual
- Lista de informes `/informes` con descarga de PDFs históricos
- Notificaciones funcionales (toggles + persistencia)
- Cuando exista `POST /api/auth/cambiar-password`: agregar formulario en Configuración → Mi perfil


---

## Estructura para sumar pantallas nuevas

Si querés agregar una pantalla `/dashboard`:

1. **API module** (si necesita backend):
   ```
   src/api/dashboard.ts
   ```

2. **Componente de pantalla:**
   ```
   src/screens/Dashboard.tsx
   ```

3. **Estilos (opcional, si tiene CSS propio):**
   ```
   src/styles/dashboard.ts
   ```

4. **Componentes específicos (opcional):**
   ```
   src/components/StatCard.tsx
   src/components/Chart.tsx
   ```

5. **Registrar la ruta en `App.tsx`:**
   ```tsx
   <Route
     path="/dashboard"
     element={
       <ProtectedRoute>
         <ShellLayout>
           <Dashboard />
         </ShellLayout>
       </ProtectedRoute>
     }
   />
   ```

6. **Agregar el NavLink en `Sidebar.tsx`:**
   ```tsx
   <NavLink to="/dashboard" className={...}>
     <svg className="nav-icon" ...>...</svg>
     Dashboard
   </NavLink>
   ```

7. **Si requiere rol específico (ej. solo Admin):** envolverlo en otro HOC que verifique `usuario.rol === 'Administrador'`.

---

## Glosario

| Término | Significado |
|---|---|
| **Hallazgo** | Algo detectado por los agentes IA durante una auditoría. Puede ser NC, OBS u OM. |
| **NC** | No Conformidad. Algo que incumple la norma ISO 9001 directamente. |
| **OBS** | Observación. Algo que no es incumplimiento pero merece atención. |
| **OM** | Oportunidad de Mejora. Sugerencia para optimizar procesos. |
| **Procedimiento** | Conjunto de etapas de un proceso de la empresa (ej: Facturación tiene etapas: Generar → Enviar → Cobrar). |
| **Etapa** | Paso específico dentro de un procedimiento. |
| **Workflow** | Pipeline de agentes IA que ejecuta una auditoría: DocumentAnalysis → ComplianceValidation → ConsistencyVerification → FindingsClassification |
| **Auditor / Operador / Administrador** | Los 3 roles del sistema (definidos en `Models/RolUsuario.cs` del backend). |

---

## Contacto

Proyecto académico — BDT Global.

**Branches activos:**
- `dev` — rama principal de desarrollo
