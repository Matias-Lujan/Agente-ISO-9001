# Agente ISO 9001 — Frontend

**Arquitectura de carpetas y función de cada archivo**

Aplicación web del sistema de auditoría de calidad de **BDT Global**. Este documento describe la estructura del frontend: qué hace cada carpeta, por qué se separó así y la función de cada archivo. Pensado como material de apoyo para la defensa del proyecto.

---

## 1. Visión general

El frontend es una **Single Page Application (SPA)** que consume una API REST del backend (.NET) y se comunica siempre por rutas `/api/*`. La aplicación nunca habla directo con la base de datos ni con el motor de IA: solo pide datos al backend y los muestra.

**Idea central de la organización:** separar por **responsabilidad**, no por pantalla. Cada carpeta tiene un único motivo para cambiar. Si cambia cómo se piden los datos, se toca `api/`; si cambia cómo se ven, se toca `screens/` o `styles/`. Esto hace el código predecible y fácil de defender: cada cosa está donde se espera.

**Seguridad:** el token JWT no se guarda en el navegador (ni en localStorage ni en sessionStorage), sino en una cookie `HttpOnly` que el JavaScript no puede leer; esto mitiga ataques XSS. El navegador la envía sola en cada pedido (`credentials: 'include'`).

**Roles:** hay tres roles —Administrador, Auditor y Operador— y la interfaz se adapta a cada uno (qué ve en el menú, a qué pantallas entra y qué datos trae).

---

## 2. Stack

| Capa | Tecnología | Versión |
|---|---|---|
| Framework | React | 19.0.0 |
| Lenguaje | TypeScript (estricto) | 5.7.2 |
| Build / dev server | Vite | 6.0.0 |
| Router | React Router DOM | 7.1.5 |
| Generación de PDF | jsPDF + html2canvas | 4.2.1 / 1.4.1 |
| Estilos | CSS-in-string + `useInjectStyle` | propio |
| Auth | JWT en cookie `HttpOnly` (`credentials: 'include'`) | propio |
| Backend | .NET 9 + MySQL + EF Core | corre en `localhost:5180` |

Sin librerías de UI externas (ni Material UI, ni Tailwind, ni shadcn). Todo el diseño es propio para mantener consistencia y control.

---

## 3. Cómo arrancarlo

### Requisitos previos

- **Node.js** 20+ y **npm**
- **.NET SDK 9** (para el backend)
- **MySQL** corriendo en `localhost:3306` con la base `iso_audit_agent` creada
- **Un usuario admin** insertado en la tabla `usuarios` (ver `sql/primer_usuario.sql` del repo)
- **Variables de entorno del backend** configuradas (`Jwt:SecretKey` como user-secret, connection string a MySQL, clave de Gemini)

### Primera vez

```bash
# Entrar al frontend
cd Agente-ISO-9001/frontend

# Instalar dependencias (incluye jsPDF y html2canvas para los export a PDF)
npm install
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

Abrí `http://localhost:5173` en el navegador. Te redirige automáticamente a `/login`. El proxy de Vite manda `/api/*` al backend, así que no hay problemas de CORS en desarrollo.

**Credenciales del admin** (según el script `primer_usuario.sql`):
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

## 4. Estructura de carpetas

Todo el código vive en `src/`. La raíz del proyecto guarda la configuración (Vite, TypeScript, dependencias).

```
frontend/
├── index.html          # Entry HTML, monta <div id="root">
├── package.json        # Dependencias y scripts npm
├── tsconfig*.json      # Config TypeScript (modo estricto)
├── vite.config.ts      # Config Vite (proxy /api → :5180)
├── assets/             # Estáticos (imágenes, logos)
└── src/
    ├── main.tsx        # Punto de entrada (monta <App/>)
    ├── App.tsx         # Router principal + layout
    ├── api/            # Acceso a datos (una función por endpoint)
    ├── login/          # Autenticación (contexto, guardia, login)
    ├── components/     # UI reutilizable y modales
    ├── screens/        # Pantallas (una por ruta)
    ├── styles/         # CSS por pantalla + compartido
    └── utils/          # Helpers transversales (estilos, navegación, PDF)
```

| Carpeta | Función |
|---|---|
| `api/` | Capa de acceso a datos. Una función por endpoint del backend, con tipos TypeScript. Es la única que sabe de URLs y de fetch. |
| `login/` | Todo lo de autenticación, aislado: contexto de sesión, guardia de rutas, pantalla de login y su API. Separado por ser un dominio transversal y sensible. |
| `components/` | Piezas de interfaz reutilizables y modales que usan varias pantallas (sidebar, modales, ejecución de auditoría). |
| `screens/` | Las pantallas completas. Cada una corresponde a una ruta de la app. Contienen la lógica de presentación. |
| `styles/` | El CSS, separado del JSX. Un archivo de estilos por pantalla, más uno compartido con los tokens del tema. |
| `utils/` | Funciones de apoyo transversales y puras (inyección de estilos, navegación por rol, exportación a PDF). |

### ¿Por qué separar así?

- **Bajo acoplamiento, alta cohesión:** cada carpeta agrupa cosas que cambian juntas y se aísla de las que no.
- **Una sola fuente de verdad para los datos:** las pantallas nunca llaman a fetch directo; siempre pasan por `api/`. Si cambia un endpoint, se corrige en un solo lugar.
- **Estilos junto a su pantalla pero fuera del JSX:** cada pantalla tiene su archivo de estilos, lo que mantiene el componente legible sin mezclar 300 líneas de CSS.
- **Testeable y defendible:** la lógica de datos (cálculos de cumplimiento, armado de hallazgos) vive en `api/` y se puede revisar sin tocar la interfaz.

---

## 5. Configuración (raíz del proyecto)

Archivos de configuración que definen cómo se construye y sirve la aplicación.

| Archivo | Función |
|---|---|
| `index.html` | Página HTML única que carga la SPA. Contiene el `<div id="root">` donde React monta todo y el `<script>` que arranca `main.tsx`. |
| `package.json` | Dependencias y scripts (dev, build, preview, lint). Dependencias de runtime: react, react-dom, react-router-dom, jspdf y html2canvas (export PDF). |
| `vite.config.ts` | Configura Vite: plugin de React, sirve los estáticos desde `assets/` y define el proxy que redirige `/api/*` al backend (localhost:5180). El proxy evita problemas de CORS en desarrollo. |
| `tsconfig*.json` | Configuración de TypeScript (referencias app/node). Activa el modo estricto, que obliga a tipar todo y atrapa errores en tiempo de compilación. |
| `assets/` | Recursos estáticos (imágenes, logos) servidos tal cual. |

---

## 6. Arranque de la aplicación

| Archivo | Función |
|---|---|
| `main.tsx` | Punto de entrada. Monta `<App/>` en el `#root` dentro de `<StrictMode>`. Antes de renderizar aplica el tema (claro/oscuro) cacheado en localStorage para evitar el "flash" de tema incorrecto al cargar. |
| `App.tsx` | Router principal. Define todas las rutas y, para cada una, qué rol puede acceder. Arma el layout con barra lateral (ShellLayout) para las pantallas internas y redirige el resto a la pantalla inicial según el rol. Inyecta los estilos compartidos. |

**Mapa de rutas y permisos:**

- `/login` — pública (sin barra lateral).
- `/proyectos` y `/proyectos/:id` — cualquier usuario autenticado (la lista la filtra el backend según el rol).
- `/informes`, `/hallazgos` y `/configuracion` — cualquier usuario autenticado.
- `/dashboard` y `/nueva-auditoria` — solo Administrador y Auditor.
- `/usuarios` — solo Administrador (ABM de usuarios).
- Cualquier otra ruta redirige a la pantalla inicial del rol (Operador → Hallazgos; resto → Dashboard).

---

## 7. Carpeta `api/` — acceso a datos

Capa que habla con el backend. Cada archivo agrupa los endpoints de un **dominio** y exporta tanto las funciones de llamada como los **tipos TypeScript** de los datos. Las pantallas importan de acá y nunca arman un fetch a mano.

| Archivo | Función |
|---|---|
| `client.ts` | Envoltorio sobre fetch usado por todos los demás. Centraliza la base `/api/*`, el envío de la cookie del JWT (`credentials: 'include'`), el parseo de errores del backend y la redirección a `/login` cuando la sesión venció (401). Exporta el objeto `api` con get/post/put/delete. |
| `proyectos.ts` | Endpoints de proyectos: listar, obtener uno, crear y asignar responsable. La visibilidad por rol la resuelve el backend (Admin/Auditor ven todos; Operador solo los asignados). |
| `auditorias.ts` | Endpoints del workflow de auditoría: crear, obtener, listar por proyecto, traer el resultado (artefactos evaluados, hallazgos, documentos) y el progreso de la ejecución. Define los tipos del estado de la auditoría y de sus resultados. |
| `hallazgos.ts` | No existe un endpoint "listar todos los hallazgos", así que esta capa los arma combinando proyectos → auditorías → hallazgos. Aporta los tipos y etiquetas de tipo/estado, y `cargarRevision()`, que calcula el cumplimiento general. |
| `informes.ts` | Endpoints de informes ya generados: listar (todos o por auditoría) y obtener uno. Un informe existe solo cuando la auditoría terminó. |
| `procedimientos.ts` | Endpoints de procedimientos internos y sus etapas (se usan al crear una auditoría y para etiquetar el cumplimiento por procedimiento). |
| `dashboard.ts` | Como tampoco hay un endpoint agregado de dashboard, esta capa combina los anteriores y calcula todas las métricas y series de los gráficos: cumplimiento general y por proyecto/procedimiento, hallazgos por tipo y por agente, evolución por mes y proyectos que requieren atención. |

**Decisión a destacar:** varias vistas (hallazgos, dashboard) necesitan datos que el backend no entrega "masticados" en un solo endpoint. En lugar de pedir un cambio al backend, el frontend compone esos datos a partir de los endpoints que sí existen. Esa lógica de composición está aislada en `api/`, no desparramada en las pantallas.

---

## 8. Autenticación y seguridad (`login/`)

La autenticación se separó del resto del dominio por ser transversal (la usan todas las pantallas) y sensible. Agrupa el estado de sesión, la protección de rutas y la pantalla de ingreso.

| Archivo | Función |
|---|---|
| `AuthContext.tsx` | Contexto global de sesión. Expone el usuario logueado, los estados de carga/verificación y las acciones (login, logout). El JWT vive en cookie HttpOnly, no en el cliente; el usuario se obtiene del backend al verificar la sesión. Provee `AuthProvider` (envuelve la app) y el hook `useAuth()`. |
| `ProtectedRoute.tsx` | Componente que envuelve las rutas privadas. Si no hay sesión, redirige a `/login`; si la ruta exige un rol (`requiereRol`) y el usuario no lo tiene, lo manda a su pantalla inicial. Es el guardia de acceso. |
| `authApi.ts` | Endpoints de autenticación y gestión de usuarios. Define los tipos centrales `Rol` y `Tema` y las operaciones de login, perfil y ABM de usuarios. |
| `Login.tsx` | Pantalla de ingreso, con un diseño split-screen animado. Conectada al AuthContext: valida el formulario (incluido el dominio del email) y dispara el login. |
| `InputField.tsx` | Campo de formulario reutilizable: label, manejo de errores y botón para mostrar/ocultar la contraseña. |
| `NetworkBackground.tsx` | Fondo animado de la pantalla de login (red de nodos sobre canvas). Reutilizable y configurable en colores y densidad. |
| `loginStyles.ts` | Estilos del login en el mismo patrón CSS-in-string del resto del proyecto. |

### Validación de dominio

Solo cuentas con email terminado en `@bdtglobal.com.ar` o `@bdtglobal.com` pueden loguearse. Esto se valida en dos lugares:

- **En el frontend** (`Login.tsx`) para UX rápida: muestra el error sin llamar al backend.
- **En el backend** (`AuthService.LoginAsync`) como validación real: un atacante no puede saltarla con `curl`.

### Identificación del usuario

Al loguearse, el backend emite un JWT (guardado en la cookie HttpOnly) con estos claims:

| Claim | Contenido |
|---|---|
| `NameIdentifier` | ID numérico del usuario |
| `Name` | Nombre completo |
| `Email` | Email |
| `Role` | `Administrador` / `Auditor` / `Operador` |

El frontend no lee el token (la cookie es HttpOnly): obtiene los datos del usuario desde el endpoint de sesión y los expone vía `AuthContext`.

### Por qué cookie HttpOnly y no localStorage/sessionStorage

Una cookie `HttpOnly` no puede ser leída por JavaScript, así que un ataque XSS no puede robar el token. Guardarlo en `localStorage` o `sessionStorage` lo dejaría accesible desde el código de la página. La duración de la sesión la controla el backend mediante la expiración de la cookie/JWT.

### Por qué redirección automática en 401

El `client.ts` intercepta cualquier respuesta 401 y manda al usuario a `/login`. Razones:

- **Sesión vencida:** cuando expira el JWT, los endpoints empiezan a responder 401.
- **Cookie ausente o inválida:** si no hay sesión válida, el backend rechaza el pedido.
- **Usuario desactivado:** si el admin desactivó al usuario, el backend rechaza el acceso aunque la cookie siga vigente.

En todos esos casos, mejor mandar al login que mostrar errores sueltos.

---

## 9. Carpeta `components/` — UI reutilizable

Piezas de interfaz que usan varias pantallas, sobre todo la navegación y los modales. Evita repetir código y mantiene consistencia visual.

| Archivo | Función |
|---|---|
| `Sidebar.tsx` | Barra lateral fija. Muestra los accesos según el rol del usuario, resalta la sección activa, deja entrar a Configuración desde el bloque de usuario y permite cerrar sesión. |
| `EjecucionAuditoria.tsx` | Sigue en vivo una auditoría en curso: consulta progreso y estado del backend y muestra el avance de cada nodo del workflow. La usa la pantalla de Nueva auditoría. |
| `HallazgoDetalleModal.tsx` | Modal con el detalle de un hallazgo. Se cierra con la tecla Escape, con clic afuera o con la X. |
| `UsuarioModal.tsx` | Modal para crear o editar un usuario (cambia los campos según el modo). Lo usa la pantalla de Usuarios. |
| `ResetPasswordModal.tsx` | Modal para que un Administrador resetee la contraseña de otro usuario sin conocer la anterior. |

---

## 10. Carpeta `screens/` — pantallas

Cada archivo es una **pantalla completa** asociada a una ruta. Contienen la lógica de presentación: piden datos a `api/`, manejan estados de carga/error y arman la vista. No traen su propia barra lateral (la pone el layout).

| Archivo | Función |
|---|---|
| `Dashboard.tsx` | Tablero del auditor (solo Admin/Auditor). Muestra KPIs, cumplimiento general con semáforo, gráficos (hallazgos por tipo y por agente, proyectos por estado, cumplimiento por proyecto y por procedimiento, evolución), proyectos que requieren atención y permite exportar todo a PDF. Gráficos hechos a mano en SVG, sin librerías. |
| `Proyectos.tsx` | Grilla de proyectos con un anillo de cumplimiento y semáforo por tarjeta. Visibilidad por rol; alta de proyecto solo para Admin. Cada tarjeta navega al detalle. |
| `ProyectoDetalle.tsx` | Detalle de un proyecto (`/proyectos/:id`): cabecera, auditorías seleccionables, resultado (artefactos evaluados, hallazgos, documentos), curva de evolución del cumplimiento y los informes asociados. |
| `Informes.tsx` | Lista los informes ya generados para verlos o descargarlos en PDF. Puede filtrarse por proyecto (al llegar desde "requieren atención" del dashboard). |
| `Hallazgos.tsx` | Pantalla de hallazgos rediseñada según el cliente: semáforo + torta de cumplimiento, deja constancia de cuántos ítems se revisaron y distingue claramente "todo OK" de "falló la consulta" (evita la pantalla en blanco ambigua). Exporta a PDF. |
| `NuevaAuditoria.tsx` | Lanza una auditoría en tres fases: carga de proyectos, selección de proyecto y etapa, y ejecución (delegando el seguimiento en vivo a `EjecucionAuditoria`). |
| `Configuracion.tsx` | Configuración del usuario en pestañas: perfil, notificaciones, integraciones y agente IA (algunas como adelanto de funciones futuras). |
| `Usuarios.tsx` | ABM de usuarios (solo Admin): tarjetas resumen, buscador y alta/edición/reseteo de contraseña mediante los modales. |

---

## 11. Carpeta `styles/` — estilos

El CSS se separó del JSX: hay **un archivo de estilos por pantalla** más uno compartido. Cada archivo exporta su CSS como texto y se inyecta con el hook `useInjectStyle`. Así el componente queda legible y el estilo viaja al lado de su pantalla.

| Archivo | Función |
|---|---|
| `shared.ts` | Estilos base y tokens del tema (colores, tipografías, layout del shell, barra superior, botones). Define las variables que usan todas las pantallas y soporta tema claro/oscuro. |
| `dashboard.ts` | Estilos del Dashboard: tarjetas de métrica, gráficos, semáforo, barras y skeletons de carga. |
| `proyectos.ts` | Estilos de la grilla de proyectos y el anillo de cumplimiento. |
| `proyectoDetalle.ts` | Estilos del detalle de proyecto y la curva de evolución. |
| `hallazgos.ts` | Estilos de la pantalla de hallazgos (banda de estado, semáforo, torta, acordeón). |
| `informes.ts` | Estilos de la lista de informes y el chip de filtro por proyecto. |
| `configuracion.ts` | Estilos de la pantalla de configuración por pestañas. |
| `usuarios.ts` | Estilos del ABM de usuarios. |

**¿Por qué CSS-in-string y no Tailwind o CSS Modules?** Para no sumar herramientas de build ni dependencias: el equipo mantiene el control total del CSS, cada pantalla lleva el suyo y el tema se centraliza con variables. Es una decisión consciente de simplicidad.

---

## 12. Carpeta `utils/` — utilidades

Funciones de apoyo transversales, en su mayoría puras (mismo input, mismo output), que no pertenecen a un dominio en particular.

| Archivo | Función |
|---|---|
| `useInjectStyle.ts` | Hook que inyecta un bloque `<style>` en el documento bajo una clave única (sin duplicar). Es la base del patrón de estilos por componente. |
| `navegacion.ts` | Calcula la pantalla inicial según el rol (Operador → Hallazgos; resto → Dashboard). Se usa en el login, en el catch-all de rutas y como respaldo del guardia. |
| `exportInformePdf.ts` | Genera el PDF de un informe (texto) con jsPDF. Compartido por Informes y por el detalle de proyecto. |
| `exportHallazgosPdf.ts` | Genera el PDF de la lista de hallazgos con jsPDF (encabezado, tabla y paginación). |
| `exportDashboardPdf.ts` | Exporta el dashboard a PDF capturando los gráficos tal cual se ven (html2canvas) y armándolos en páginas A4 con jsPDF, sección por sección para no cortar ningún gráfico. |

---

## 13. Cómo se conecta todo

Un recorrido típico ayuda a ver cómo colaboran las capas:

1. El usuario entra a `/dashboard`.
2. El guardia `ProtectedRoute` verifica, vía `AuthContext`, que haya sesión y que el rol sea Admin o Auditor.
3. La pantalla `Dashboard.tsx` pide los datos a `api/dashboard.ts`.
4. Esa capa combina varios endpoints (a través de `api/client.ts`, que adjunta la cookie del JWT) y calcula las métricas.
5. La pantalla dibuja los gráficos en SVG con sus estilos de `styles/dashboard.ts`.
6. Si el usuario exporta, `utils/exportDashboardPdf.ts` captura la vista y arma el PDF.

Cada paso toca una sola capa, y cada capa hace una sola cosa. Esa es la idea que sostiene toda la arquitectura del frontend.