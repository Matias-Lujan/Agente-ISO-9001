## 🧪 Guía de Pruebas del Agente de Validación

### 1️⃣ Pruebas Unitarias (Recomendado para desarrollo)

#### Ejecutar TODOS los tests
```bash
cd backend/ISOAuditAgent.Tests
dotnet test
```

**Resultado esperado:**
```
Resumen de pruebas: total: 9; con errores: 0; correcto: 9; omitido: 0; duración: 0,9 s
✅ PASS
```

#### Ejecutar UN test específico
```bash
dotnet test --filter "ValidateProcessAsync_ShouldReturnHallazgos_WithMockData"
```

#### Ejecutar tests de un grupo
```bash
# Solo tests de funcionalidad
dotnet test --filter "Mock"

# Solo tests de arquitectura
dotnet test --filter "Architecture"
```

#### Ver output detallado
```bash
dotnet test --logger "console;verbosity=detailed"
```

---

### 2️⃣ Prueba Manual con API REST (Recomendado para QA)

#### Paso 1: Iniciar la API

```bash
cd backend/ISOAuditAgent.API
dotnet run
```

**Output esperado:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Now listening on: http://localhost:5000
Press CTRL+C to stop, CTRL+BREAK to pause
```

#### Paso 2: Probar Health Check (en otra terminal)

```bash
curl -X GET "http://localhost:5000/api/compliancevalidation/health"
```

**Response esperada (200 OK):**
```json
{
  "status": "healthy",
  "timestamp": "2026-04-29T15:30:00Z"
}
```

#### Paso 3: Validar Proceso Completo

```bash
curl -X POST "http://localhost:5000/api/compliancevalidation/validate?projectId=TEST-PROJECT-001&procesoId=1"
```

**Response esperada (200 OK) - Lista vacía (sin Kernel real):**
```json
{
  "exitosoValidacion": true,
  "hallazgosCount": 0,
  "hallazgos": [],
  "fechaAnalisis": "2026-04-29T15:30:00Z",
  "projectId": "TEST-PROJECT-001",
  "procesoId": 1
}
```

> **Nota:** Retorna lista vacía porque el Kernel no está configurado con clave OpenAI válida. Con una clave real, retornaría hallazgos detectados.

---

### 3️⃣ Pruebas de Error (Validar manejo)

#### Parámetro projectId vacío
```bash
curl -X POST "http://localhost:5000/api/compliancevalidation/validate?projectId=&procesoId=1"
```

**Response esperada (400 Bad Request):**
```json
{
  "mensaje": "El parámetro 'projectId' es requerido y no puede estar vacío.",
  "codigo": "PARAM_INVALID"
}
```

#### Parámetro procesoId inválido
```bash
curl -X POST "http://localhost:5000/api/compliancevalidation/validate?projectId=PROJ-001&procesoId=-1"
```

**Response esperada (400 Bad Request):**
```json
{
  "mensaje": "El parámetro 'procesoId' debe ser un número mayor a 0.",
  "codigo": "PARAM_INVALID"
}
```

#### Parámetro procesoId faltante
```bash
curl -X POST "http://localhost:5000/api/compliancevalidation/validate?projectId=PROJ-001"
```

**Response esperada (400 Bad Request):**
```json
{
  "mensaje": "El parámetro 'procesoId' debe ser un número mayor a 0.",
  "codigo": "PARAM_INVALID"
}
```

---

### 4️⃣ Pruebas con Postman/Insomnia

#### Crear colección de pruebas

**1. Health Check**
- Método: GET
- URL: `http://localhost:5000/api/compliancevalidation/health`
- Headers: (ninguno)
- Body: (vacío)

**2. Validar Proceso (Caso Happy Path)**
- Método: POST
- URL: `http://localhost:5000/api/compliancevalidation/validate?projectId=TEST-PROJECT-001&procesoId=1`
- Headers: (automático)
- Body: (vacío)

**3. Validar Proceso Diferente**
- Método: POST
- URL: `http://localhost:5000/api/compliancevalidation/validate?projectId=PROJ-AUDIT&procesoId=2`
- Headers: (automático)
- Body: (vacío)

**4. Error: ProjectId Vacío**
- Método: POST
- URL: `http://localhost:5000/api/compliancevalidation/validate?projectId=&procesoId=1`
- Headers: (automático)
- Body: (vacío)
- **Expected Status:** 400

---

### 5️⃣ Pruebas de Datos Mock

Los datos mock están en el código. Para inspeccionarlos:

#### Ver tareas de Trello mock
**Archivo:** `backend/ISOAuditAgent.API/Integrations/MCP/MockMcpClient.cs`
**Método:** `GetTareasTrelloAsync()`

```csharp
// 4 tareas mockeadas:
- TRE-001: "Documentación de Procesos" - En Progreso (Juan Pérez)
- TRE-002: "Validación de Registros" - Completado (María García)
- TRE-003: "Auditoría Interna" - Por Iniciar (Carlos López)
- TRE-004: "Capacitación del Equipo" - En Progreso (Juan Pérez)
```

#### Ver registros de Clockify mock
**Método:** `GetRegistrosClockifyAsync()`

```csharp
// 7 registros mockeados:
- CLO-001 a CLO-005: Registros normales
- CLO-006: ⚠️ INCONSISTENCIA - TRE-999 (no existe en Trello)
- CLO-007: ⚠️ INCONSISTENCIA - Usuario Carlos vs Responsable Juan
```

#### Ver reglas de validación mock
**Archivo:** `backend/ISOAuditAgent.API/Repositories/MockReglaValidacionRepository.cs`
**Método:** `GetReglasByProcesoAsync()`

```csharp
// 9 reglas para proceso 1:
1. Registros deben corresponder a tareas (Obligatorio)
2. Responsable coherente (Obligatorio)
3. No exceden vencimiento (Obligatorio)
4. Tareas completadas tienen registros (Obligatorio)
5. Registros recientes (Opcional)
6. Límite 80 horas (Obligatorio)
// ... más reglas
```

---

### 6️⃣ Pruebas de Flujo Completo (Paso a Paso)

#### Escenario: Usuario valida Proyecto 1

```bash
# Terminal 1: Iniciar API
cd backend/ISOAuditAgent.API
dotnet run

# Terminal 2: Ejecutar validación
curl -X POST "http://localhost:5000/api/compliancevalidation/validate?projectId=TEST-PROJECT-001&procesoId=1"

# Flujo interno (log esperado):
# 1. Controller recibe parámetros
# 2. Controller valida parámetros ✓
# 3. Agent.ValidateProcessAsync() inicia
# 4. MCP obtiene 4 tareas de Trello ✓
# 5. MCP obtiene 7 registros de Clockify ✓
# 6. Repository obtiene 9 reglas de validación ✓
# 7. Se agrupan reglas (Obligatorias + Opcionales)
# 8. Se construye ContextoValidacion
# 9. AnalizarConLLMAsync() inicia
# 10. PromptConstructor crea prompt dinámico
# 11. Kernel.InvokePromptAsync() → (Kernel null en tests)
# 12. Retorna lista de hallazgos
# 13. Controller retorna HTTP 200 OK
```

---

### 7️⃣ Debugging (Si hay problemas)

#### Ver logs en consola
La API imprime logs en tiempo real:
```
info: Iniciando validación: projectId=TEST-PROJECT-001, procesoId=1
info: Validación completada: 0 hallazgos detectados
```

#### Verificar datos mock
Editar `MockMcpClient.cs` y agregar `Console.WriteLine()`:
```csharp
public async Task<List<TrelloTaskDto>> GetTareasTrelloAsync(string projectId)
{
    Console.WriteLine($"📋 Obteniendo {tareasMock.Count} tareas de Trello");
    return Task.FromResult(tareasMock);
}
```

#### Ejecutar tests con verbose
```bash
dotnet test --verbosity=detailed --logger "console;verbosity=detailed"
```

---

### 8️⃣ Checklist de Pruebas Completas

- ✅ Health Check responde 200 OK
- ✅ Validación básica retorna 200 OK
- ✅ Parámetro projectId vacío retorna 400
- ✅ Parámetro procesoId=0 retorna 400
- ✅ Respuesta tiene estructura correcta
- ✅ Todos los 9 tests pasan
- ✅ No hay errores en compilación
- ✅ Logs se imprimen en consola
- ✅ MockMcpClient retorna datos correctos
- ✅ MockReglaValidacionRepository retorna reglas correctas

---

### 9️⃣ Pruebas con Datos Reales (Futuro)

Una vez integrado con MCP/BD real:

```bash
# Los datos reales vendrían de:
# - Trello API (vía MCP Server)
# - Clockify API (vía MCP Server)
# - BD MySQL (vía Entity Framework Core)

# Y el LLM retornaría hallazgos reales como:
{
  "descripcion": "La tarea TRE-001 está asignada a Juan Pérez en Trello, pero Carlos López registró horas en Clockify",
  "fuente": "Trello vs Clockify",
  "idTareaRelacionada": "TRE-001",
  "reglaIncumplida": "El responsable en Trello debe ser coherente con quien registra tiempo",
  "severidad": "Media"
}
```

---

### 🔟 Monitoreo en Tiempo Real

#### Ver logs de la API mientras se valida
```bash
# Terminal 1
cd backend/ISOAuditAgent.API
dotnet run

# Verás:
# info: [timestamp] Iniciando validación: projectId=..., procesoId=...
# info: [timestamp] Validación completada: X hallazgos detectados
# error: [timestamp] (si hay error)
```

#### Modificar nivel de logging
En `Program.cs`:
```csharp
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddConsole();
```

---

## Resumen Rápido

**Para desarrolladores:**
```bash
dotnet test  # ✅ 9/9 tests pasan
```

**Para QA:**
```bash
dotnet run   # Inicia API en http://localhost:5000
curl -X POST "http://localhost:5000/api/compliancevalidation/validate?projectId=PROJ-001&procesoId=1"
```

**Para ver datos mock:**
- Abrir `MockMcpClient.cs` línea ~20
- Abrir `MockReglaValidacionRepository.cs` línea ~20

---

¡Listo para probar! 🚀
