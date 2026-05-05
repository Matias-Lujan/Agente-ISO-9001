# Agente de Validación de Procesos ISO 9001

## 📋 Descripción General

Sistema de Auditoría ISO 9001 con un **Agente de Validación de Procesos** que cruza datos de Trello (tareas planificadas) y Clockify (horas registradas) para detectar inconsistencias y desvíos.

**Arquitectura estricta:**
- ✅ Sin acceso directo a APIs (todo vía MCP - Model Context Protocol)
- ✅ Solo detecta inconsistencias (NO clasifica)
- ✅ Reglas dinámicas desde BD MySQL
- ✅ IA: Semantic Kernel + OpenAI (migrable a Google Gemini)

---

## 🏗️ Arquitectura

### Flujo de Datos

```
Trello (Tasks)  ──┐
                  ├─ MCP Server ─ IMcpClient ─ ComplianceValidationAgent
Clockify (Hours)─┤
                  └─ Semantic Kernel (OpenAI) ─ Hallazgos
                      
BD MySQL ─ IReglaValidacionRepository ─ ComplianceValidationAgent
```

### Principios Arquitectónicos

1. **Sin acceso directo a APIs**
   - El agente NO se conecta directamente a Trello, Clockify o BD
   - Todo pasa por interfaces: `IMcpClient`, `IReglaValidacionRepository`
   - Beneficio: Testeable, desacoplado, seguro

2. **Solo detección, no clasificación**
   - Este agente detecta: "Hay una inconsistencia X"
   - Agente de Clasificación dirá: "Esto es una No Conformidad"
   - Separación de responsabilidades clara

3. **Patrón Strategy**
   - Reglas agrupadas en: Obligatorias vs Opcionales
   - Cada tipo se procesa de forma diferente

4. **LLM como analizador**
   - Semantic Kernel + OpenAI/Gemini
   - Prompt con guardrails explícitos
   - JSON como formato de salida determinista

---

## 📦 Proyectos

### ISOAuditAgent.API
- **Framework**: .NET 9.0
- **Nugets principales**:
  - `Microsoft.SemanticKernel` (1.30.0)
  - `Microsoft.SemanticKernel.Connectors.OpenAI` (1.30.0)

**Estructura:**
```
ISOAuditAgent.API/
├── Agents/
│   └── ComplianceValidation/
│       ├── IComplianceValidationAgent.cs      (Interfaz)
│       ├── ComplianceValidationAgent.cs       (Implementación)
│       ├── PromptConstructor.cs               (Constructor del prompt)
│       └── ContextoValidacion.cs              (Datos para análisis)
│
├── Integrations/MCP/
│   ├── IMcpClient.cs                         (Interfaz)
│   └── MockMcpClient.cs                      (Mock para tests)
│
├── Repositories/
│   ├── IReglaValidacionRepository.cs          (Interfaz)
│   └── MockReglaValidacionRepository.cs       (Mock para tests)
│
├── DTOs/
│   ├── TrelloTaskDto.cs                       (Tareas de Trello)
│   └── ClockifyRecordDto.cs                   (Registros de tiempo)
│
├── Models/
│   ├── ReglaValidacion.cs                     (Reglas de BD)
│   └── ValidationFinding.cs                   (Hallazgos)
│
├── Controllers/
│   └── ComplianceValidationController.cs      (Endpoint HTTP)
│
└── Program.cs                                  (Configuración)
```

### ISOAuditAgent.Tests
- **Framework**: xUnit
- **Cobertura**:
  - Tests de funcionalidad del agente
  - Tests de arquitectura y restricciones
  - Tests de datos mock

---

## 🚀 Uso

### 1. Compilar

```bash
cd backend/ISOAuditAgent.API
dotnet build
```

### 2. Ejecutar Tests

```bash
cd backend/ISOAuditAgent.Tests
dotnet test
```

### 3. Ejecutar API

```bash
cd backend/ISOAuditAgent.API
dotnet run
```

**Output esperado:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
```

### 4. Consumir el Endpoint

**Health Check:**
```bash
curl -X GET https://localhost:5001/api/compliancevalidation/health
```

**Validar Proceso:**
```bash
curl -X POST "https://localhost:5001/api/compliancevalidation/validate?projectId=PROJ-001&procesoId=1"
```

**Response (200 OK):**
```json
{
  "exitosoValidacion": true,
  "hallazgosCount": 2,
  "hallazgos": [
    {
      "id": "uuid...",
      "descripcion": "La tarea TRE-001 está asignada a Juan Pérez en Trello, pero Carlos López registra horas...",
      "fuente": "Trello vs Clockify",
      "idTareaRelacionada": "TRE-001",
      "reglaIncumplida": "El responsable en Trello debe ser coherente con quien registra tiempo en Clockify",
      "detallesJSON": "{...}",
      "severidad": "Media",
      "fechaDeteccion": "2026-04-29T15:30:00Z",
      "procesoId": 1,
      "projectId": "PROJ-001"
    }
  ],
  "fechaAnalisis": "2026-04-29T15:30:00Z",
  "projectId": "PROJ-001",
  "procesoId": 1
}
```

---

## ⚙️ Configuración

### appsettings.json

```json
{
  "OpenAiApiKey": "{{REEMPLAZAR}}",
  "OpenAiModelId": "gpt-4o",
  "ComplianceValidation": {
    "Timeout": 30000,
    "MaxRetries": 3
  }
}
```

### Variables de Entorno (Recomendado)

```bash
# PowerShell
$env:OPENAI_API_KEY="sk-..."
$env:OPENAI_MODEL_ID="gpt-4o"

# Bash
export OPENAI_API_KEY="sk-..."
export OPENAI_MODEL_ID="gpt-4o"
```

---

## 🧪 Datos Mock Incluidos

El sistema viene precargado con datos mock para testing **sin** dependencias externas:

### Tareas Trello (MockMcpClient)
- **TRE-001**: "Documentación de Procesos" - En Progreso (Juan Pérez)
- **TRE-002**: "Validación de Registros" - Completado (María García)
- **TRE-003**: "Auditoría Interna" - Por Iniciar (Carlos López)
- **TRE-004**: "Capacitación del Equipo" - En Progreso (Juan Pérez)

### Registros Clockify (MockMcpClient)
- **CLO-001 a CLO-005**: Registros normales
- **CLO-006**: ⚠️ Registro huérfano (TRE-999 no existe en Trello)
- **CLO-007**: ⚠️ Discrepancia responsable (usuario Carlos, pero tarea asignada a Juan)

### Reglas de Validación (MockReglaValidacionRepository)

**Para Proceso 1 (Gestión de Tareas):**
1. Registros deben corresponder a tareas (Obligatorio)
2. Responsable coherente (Obligatorio)
3. Registros no exceden vencimiento (Obligatorio)
4. Tareas completadas tienen registros (Obligatorio)
5. Registros recientes para tareas activas (Opcional)
6. Límite de horas <= 80 (Obligatorio)

---

## 🔍 Tipos de Inconsistencias Detectadas

| # | Tipo | Ejemplo | Severidad |
|---|------|---------|-----------|
| 1 | **Registro Huérfano** | CLO-006 → TRE-999 (no existe) | Alta |
| 2 | **Discrepancia Responsable** | TRE-001: Juan vs CLO-007: Carlos | Media |
| 3 | **Fuera de Plazo** | Registro el 2024-01-20, vencimiento 2024-01-15 | Alta |
| 4 | **Sin Registros** | Tarea completada sin evidencia de trabajo | Alta |
| 5 | **Horas Excesivas** | Suma > 80 horas para una tarea | Media |
| 6 | **Inactividad Sospechosa** | Tarea "En Progreso" sin registros recientes | Baja |

---

## 🛡️ Guardrails del LLM

El prompt enviado a OpenAI/Gemini incluye guardrails explícitos:

```
❌ NO CLASIFICAR
   - Este agente SOLO detecta desvíos
   - La clasificación en "No Conformidad", "Observación", etc. es responsabilidad del Agente de Clasificación

✓ FORMATO JSON ESTRICTO
   - Retorna SOLO un array JSON
   - Si no hay inconsistencias: []

✓ DATOS OBJETIVOS
   - Comparación de IDs, fechas, responsables
   - Presencia/ausencia de registros
```

---

## 📝 PASOS de Implementación (Ya Completados)

### ✅ PASO 1: Entidades y DTOs
- `TrelloTaskDto`, `ClockifyRecordDto`, `ReglaValidacion`, `ValidationFinding`
- ✅ 4 archivos creados en DTOs/ y Models/

### ✅ PASO 2: Contrato MCP
- `IMcpClient`, `MockMcpClient`
- ✅ 2 archivos creados en Integrations/MCP/

### ✅ PASO 3: Repositorio de Reglas
- `IReglaValidacionRepository`, `MockReglaValidacionRepository`
- ✅ 2 archivos creados en Repositories/

### ✅ PASO 4: Estructura del Agente
- `IComplianceValidationAgent`, `ComplianceValidationAgent`
- Patrón Strategy (reglas Obligatorias vs Opcionales)
- ✅ 2 archivos creados en Agents/ComplianceValidation/

### ✅ PASO 5: Configuración Semantic Kernel
- Registración de Kernel en Program.cs
- OpenAI ChatCompletion integrado
- ✅ Compilación exitosa

### ✅ PASO 6: Diseño del Prompt
- `PromptConstructor` con 9 secciones
- Guardrails, reglas, datos, instrucciones, ejemplo, estructura JSON
- ✅ 1 archivo creado en Agents/ComplianceValidation/

### ✅ PASO 7: Deserialización y Retorno
- `ComplianceValidationController` - Endpoint HTTP
- DTOs de respuesta: `ComplianceValidationResponse`, `ErrorResponse`
- Tests unitarios con xUnit (11 tests)
- ✅ 1 controlador + Tests compilados

---

## 🔄 Flujo Completo de una Validación

```
1. Cliente invoca POST /api/compliancevalidation/validate?projectId=...&procesoId=...
   │
2. Controller valida parámetros
   │
3. Agent.ValidateProcessAsync()
   ├─ MCP obtiene tareas de Trello
   ├─ MCP obtiene registros de Clockify
   ├─ Repository obtiene reglas de BD
   │
4. Agrupa reglas (Obligatorias + Opcionales)
   │
5. Construye ContextoValidacion
   │
6. AnalizarConLLMAsync()
   ├─ PromptConstructor genera prompt dinámico
   ├─ Kernel invoca OpenAI
   ├─ Extrae JSON de respuesta
   ├─ Deserializa a ValidationFinding
   │
7. Enriquece hallazgos (ProcesoId, ProjectId)
   │
8. Retorna HTTP 200 OK con ComplianceValidationResponse
```

---

## 🚨 Errores Comunes

### Error: "OPENAI_API_KEY no está configurada"
**Solución:** Configurar variable de entorno o appsettings.json
```bash
$env:OPENAI_API_KEY="sk-..."
```

### Error: "Ninguna sobrecarga para el método..."
**Solución:** Verificar versiones de NuGets (xUnit, Semantic Kernel)

### Tests fallan con Kernel nulo
**Solución:** MockMcpClient y MockReglaValidacionRepository no requieren Kernel real

---

## 📚 Documentación Detallada

- [PASO 1 - Entidades y DTOs](./PASO1_ENTIDADES_DTOS.md) *(no creado, ver commit)*
- [PASO 2 - Contrato MCP](./PASO2_MCP_CONTRACT.md) *(no creado, ver commit)*
- [PASO 3 - Repositorio de Reglas](./PASO3_REPOSITORY.md) *(no creado, ver commit)*
- [PASO 4 - Estructura del Agente](./PASO4_AGENT_STRUCTURE.md) *(no creado, ver commit)*
- [PASO 5 - Semantic Kernel](./PASO5_SEMANTIC_KERNEL.md)
- [PASO 6 - Diseño del Prompt](./PASO6_DESIGN_PROMPT.md)
- [PASO 7 - Deserialización y Retorno](./PASO7_DESERIALIZATION_RETURN.md)

---

## 🤝 Integración Futura

### Pasos para producción:

1. **MCP Real**: Reemplazar `MockMcpClient` con cliente real que consulte MCP Server
2. **BD Real**: Reemplazar `MockReglaValidacionRepository` con Entity Framework Core
3. **Google Gemini**: Cambiar `AddOpenAIChatCompletion()` a `AddGoogleGenerativeAI()` cuando conector esté disponible
4. **Swagger**: Agregar `builder.Services.AddSwaggerGen()`
5. **Docker**: Crear Dockerfile y docker-compose.yml
6. **Logging**: Reemplazar Console con Serilog
7. **Caching**: Agregar Redis para caché de reglas

---

## 📞 Soporte

**Arquitecto:** Desarrollador Senior .NET 9
**Fecha:** Abril 29, 2026
**Estado:** ✅ Completamente funcional con datos mock

---

## 📄 Licencia

Proyecto interno para Auditoría ISO 9001
