# Agente Inteligente de Auditoría ISO 9001 — BDT Global

Trabajo final — Tecnicatura en Análisis de Sistemas (ORT).

Sistema backend para ejecutar auditorías documentales sobre proyectos de BDT Global, usando .NET Web API, MySQL, EF Core, Microsoft Agent Framework, MCP, Google Drive y agentes LLM.

---

## Estado actual del backend

El backend MVP ya permite:

- Crear una auditoría desde API REST.
- Encolar la auditoría para ejecución asíncrona.
- Ejecutar el workflow MAF de auditoría.
- Leer el tailoring FR-29 desde Google Drive.
- Evaluar artefactos esperados.
- Generar hallazgos.
- Persistir resultado en MySQL.
- Consultar metadata de auditoría.
- Consultar resultado completo mediante endpoint `_smoke`.

---

## Stack principal

- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- MySQL
- Microsoft Agent Framework
- MCP
- Gemini
- Google Drive API

---

## Estructura principal del backend

El backend vive en:

```text
backend/ISOAuditAgent.API
```

Carpetas principales:

```text
Agents/
├── Contracts/                  # DTOs que viajan entre nodos del workflow
├── DocumentAnalysis/            # Lectura de tailoring, Drive, parsing y armado de artefactos
│   ├── Drive/
│   ├── Parsing/
│   └── Tailoring/
├── ComplianceValidation/        # Validación tailoring vs ejecución
├── ConsistencyVerification/     # Validaciones estructurales/formales
├── FindingsClassification/      # Clasificación NC / OBS / OM
└── Orchestrator/                # Workflow MAF
    ├── Execution/               # Cola, worker y runner
    ├── Nodes/                   # Nodos del workflow
    ├── Persistence/             # Persistencia del resultado
    └── Workflow/                # Factory del grafo y ensamble final

Data/                           # DbContext EF Core
Models/                         # Entidades y enums EF
Repositories/                   # Repositorios + UnitOfWork
Endpoints/                      # Minimal APIs reales
Integrations/
├── LLM/                         # Gemini / AIAgent wiring
└── MCP/Drive/                   # Integración Drive vía MCP/tools
Migrations/                     # Migraciones EF Core
```

---

## Requisitos

- .NET 9 SDK
- MySQL 8.x local
- `dotnet-ef`
- Acceso a Google Drive mediante service account
- API key de Gemini

Instalar `dotnet-ef` si no está instalado:

```bash
dotnet tool install --global dotnet-ef
```

---

## Configuración de base de datos

La connection string no se commitea. Cada integrante debe cargarla en User Secrets.

Desde:

```bash
cd backend/ISOAuditAgent.API
```

Ejecutar:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=iso_audit_agent;User=root;Password=PASSWORD;"
```

Reemplazar `PASSWORD` por la contraseña local de MySQL.

---

## Configuración de Gemini

La API key de Gemini tampoco se commitea. Cargarla en User Secrets:

```bash
dotnet user-secrets set "Gemini:ApiKey" "TU_API_KEY"
```

---

## Configuración de Google Drive

El backend usa una service account para leer archivos de Drive.

No commitear nunca el JSON real de la service account.

Ubicación local sugerida:

```text
backend/ISOAuditAgent.API/secrets/google-service-account.json
```

La carpeta `secrets/` debe estar ignorada por Git.

Recomendado en `.gitignore`:

```gitignore
# Secrets / credentials
secrets/
**/secrets/
*.credentials.json
*service-account*.json
```

Si el archivo ya fue trackeado por Git, sacarlo del tracking sin borrarlo localmente:

```bash
git rm --cached backend/ISOAuditAgent.API/secrets/google-service-account.json
```

---

## Crear o actualizar la base

Desde:

```bash
cd backend/ISOAuditAgent.API
```

Ejecutar:

```bash
dotnet ef database update
```

Esto crea o actualiza la base local `iso_audit_agent` aplicando las migraciones.

---

## Seed de demo

Para dejar la base con datos mínimos de demo/E2E, ejecutar:

```text
backend/ISOAuditAgent.API/db/seed-demo.sql
```

Desde MySQL Workbench, abrir el archivo y ejecutarlo sobre la base `iso_audit_agent`.

El seed carga:

- configuración mínima `path_carpeta_templates`
- procedimiento PR 11-13
- etapa Planificación
- usuario demo
- proyecto demo con `drive_folder_id`
- relación proyecto_usuario
- 6 artefactos esperados

Al finalizar, el script muestra queries de verificación con los IDs reales que deben usarse para ejecutar una auditoría:

```text
proyectoId
etapaId
usuarioId
```

Luego usar esos IDs en:

```http
POST /api/auditorias
```

Ejemplo:

```json
{
  "proyectoId": "<id_del_proyecto>",
  "etapaId": "<id_de_planificacion>",
  "usuarioId": "<id_del_usuario>"
}
```

El seed es idempotente: puede ejecutarse más de una vez sin duplicar los datos de demo.

---

## Cambios en el modelo

Si se modifica una entidad EF, generar una migración:

```bash
dotnet ef migrations add NombreDescriptivoDelCambio
```

Después aplicar la migración localmente:

```bash
dotnet ef database update
```

La migración generada debe commitearse.

---

## Ejecutar backend

Desde:

```bash
cd backend/ISOAuditAgent.API
```

Ejecutar:

```bash
dotnet restore
dotnet build
dotnet run
```

La URL local puede variar según `launchSettings.json`. En las pruebas actuales se usó:

```text
http://localhost:5180
```

---

## Endpoints principales

### Crear auditoría

```http
POST /api/auditorias
```

Ejemplo:

```json
{
  "proyectoId": "<id_del_proyecto>",
  "etapaId": "<id_de_planificacion>",
  "usuarioId": "<id_del_usuario>"
}
```

Respuesta esperada:

```http
202 Accepted
```

La auditoría queda en estado `EnCurso` y se ejecuta en background.

---

### Consultar estado de auditoría

```http
GET /api/auditorias/{id}
```

Ejemplo:

```http
GET /api/auditorias/12
```

Respuesta esperada cuando termina bien:

```json
{
  "id": 12,
  "proyectoId": 1,
  "etapaId": 1,
  "usuarioId": 1,
  "estado": "Completada",
  "fechaInicioUtc": "...",
  "fechaFinalizacionUtc": "..."
}
```

---

### Consultar resultado completo por smoke

Endpoint temporal para demo/debug:

```http
GET /api/_smoke/auditorias/{id}/resultado
```

Ejemplo:

```http
GET /api/_smoke/auditorias/12/resultado
```

Devuelve:

- metadata de auditoría
- contadores
- artefactos evaluados
- hallazgos por artefacto
- documentos analizados por artefacto

Este endpoint es `_smoke`; no reemplaza al endpoint real definitivo de resultados/informes.

---

## Smokes

El proyecto conserva endpoints `_smoke` para pruebas de integración y diagnóstico.

No eliminarlos sin coordinar con el equipo, porque todavía sirven para:

- validar wiring de DI
- probar Gemini
- probar agentes
- probar Drive
- probar ejecución de workflow
- inspeccionar resultado persistido

---

## Archivos que no deben commitearse

Antes de commitear, revisar:

```bash
git status
```

No commitear:

```text
backend/ISOAuditAgent.API/secrets/google-service-account.json
bin/
obj/
.vs/
```

Validar si hay secrets trackeados:

```bash
git ls-files | findstr secrets
```

Si aparece un secret real, sacarlo del tracking:

```bash
git rm --cached RUTA_DEL_ARCHIVO
```

---

## Comandos útiles

Build:

```bash
dotnet build
```

Run:

```bash
dotnet run
```

Aplicar migraciones:

```bash
dotnet ef database update
```

Ver rama actual:

```bash
git branch --show-current
```

Ver estado Git:

```bash
git status
```
