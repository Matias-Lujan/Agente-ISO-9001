## 📖 Referencia Rápida para Desarrolladores

### Inicio Rápido (5 minutos)

```bash
# 1. Compilar
cd backend
dotnet build

# 2. Ejecutar tests (verifica que todo funciona)
cd ISOAuditAgent.Tests
dotnet test

# 3. Ejecutar API
cd ../ISOAuditAgent.API
dotnet run

# 4. Invocar endpoint (en otra terminal)
curl -X POST "http://localhost:5000/api/compliancevalidation/validate?projectId=PROJ-001&procesoId=1"
```

---

### Estructura de Carpetas Clave

```
backend/
├── ISOAuditAgent.API/
│   ├── Agents/ComplianceValidation/
│   │   ├── IComplianceValidationAgent.cs     ← INTERFAZ del agente
│   │   ├── ComplianceValidationAgent.cs      ← IMPLEMENTACIÓN
│   │   ├── PromptConstructor.cs              ← Constructor del prompt para LLM
│   │   └── ContextoValidacion.cs             ← Datos para análisis
│   │
│   ├── Controllers/
│   │   └── ComplianceValidationController.cs ← ENDPOINT HTTP
│   │
│   ├── Integrations/MCP/
│   │   ├── IMcpClient.cs                     ← INTERFAZ MCP
│   │   └── MockMcpClient.cs                  ← IMPLEMENTACIÓN MOCK
│   │
│   ├── Repositories/
│   │   ├── IReglaValidacionRepository.cs     ← INTERFAZ Reglas
│   │   └── MockReglaValidacionRepository.cs  ← IMPLEMENTACIÓN MOCK
│   │
│   ├── DTOs/
│   │   ├── TrelloTaskDto.cs                  ← Tareas de Trello
│   │   └── ClockifyRecordDto.cs              ← Registros de Clockify
│   │
│   ├── Models/
│   │   ├── ReglaValidacion.cs                ← Reglas de BD
│   │   └── ValidationFinding.cs              ← HALLAZGOS (output)
│   │
│   ├── Program.cs                            ← CONFIGURACIÓN central
│   ├── appsettings.json                      ← VARIABLES DE CONFIGURACIÓN
│   └── ISOAuditAgent.API.csproj              ← REFERENCIAS NUGET
│
└── ISOAuditAgent.Tests/
    └── ComplianceValidationAgentTests.cs     ← TESTS UNITARIOS
```

---

### Flujo de Datos Simplificado

```
POST /api/compliancevalidation/validate
   ↓
ComplianceValidationController
   ├─ Valida parámetros
   └─ Llama a → IComplianceValidationAgent.ValidateProcessAsync()
      ├─ IMcpClient.GetTareasTrelloAsync()       ← Obtiene tareas
      ├─ IMcpClient.GetRegistrosClockifyAsync()  ← Obtiene registros
      ├─ IReglaValidacionRepository.GetReglasByProcesoAsync() ← Obtiene reglas
      │
      └─ AnalizarConLLMAsync()
         ├─ PromptConstructor.ConstruirPrompt()
         ├─ Kernel.InvokePromptAsync(prompt)    ← Envía a OpenAI
         ├─ ExtraerJsonDeRespuesta()
         └─ DeserializarHallazgos()              ← Retorna List<ValidationFinding>
   ↓
HTTP 200 + ComplianceValidationResponse
```

---

### Archivos Principales a Conocer

| Archivo | Responsabilidad |
|---------|-----------------|
| `ComplianceValidationAgent.cs` | Orquestación del análisis |
| `PromptConstructor.cs` | Construcción dinámica del prompt |
| `ComplianceValidationController.cs` | Endpoint HTTP |
| `IMcpClient.cs` + `MockMcpClient.cs` | Obtención de datos (Trello/Clockify) |
| `IReglaValidacionRepository.cs` + `MockReglaValidacionRepository.cs` | Obtención de reglas |
| `Program.cs` | DI Container + configuración |

---

### Configuración Necesaria

#### Para Desarrollo (Con datos mock)
```json
// appsettings.json
{
  "OpenAiApiKey": "{{REEMPLAZAR}}",
  "OpenAiModelId": "gpt-4o"
}
```

#### Variable de Entorno Recomendada
```bash
$env:OPENAI_API_KEY="sk-..."
```

---

### Testing

#### Ejecutar todos los tests
```bash
cd backend/ISOAuditAgent.Tests
dotnet test
```

#### Ejecutar un test específico
```bash
dotnet test --filter "ValidateProcessAsync_ShouldReturnHallazgos_WithMockData"
```

#### Tests disponibles (11 total)
- `ValidateProcessAsync_ShouldReturnHallazgos_WithMockData`
- `GetTareasTrelloAsync_ShouldReturnMockTasks`
- `GetRegistrosClockifyAsync_ShouldReturnMockRecords`
- `GetReglasByProcesoAsync_ShouldReturnValidationRules`
- `ValidationRules_ShouldHaveDescriptionAndCriteria`
- `DetectHuerfanoRecords_ShouldIdentifyMissingTasks`
- `DetectResponsibleMismatch_ShouldIdentifyDifferentResponsible`
- `Agent_ShouldNotAccessExternalAPIsDirectly`
- `Agent_ShouldInjectSemanticKernel`
- ... (2 más de arquitectura)

---

### API Endpoints

#### Validar Proceso
```
POST /api/compliancevalidation/validate?projectId={projectId}&procesoId={procesoId}

Parámetros:
  - projectId (string, requerido): ID del proyecto
  - procesoId (int, requerido): ID del proceso ISO

Respuesta 200 OK:
{
  "exitosoValidacion": true,
  "hallazgosCount": 2,
  "hallazgos": [...],
  "fechaAnalisis": "2026-04-29T15:30:00Z",
  "projectId": "...",
  "procesoId": 1
}

Respuesta 400 Bad Request:
{
  "mensaje": "El parámetro 'projectId' es requerido...",
  "codigo": "PARAM_INVALID"
}
```

#### Health Check
```
GET /api/compliancevalidation/health

Respuesta:
{
  "status": "healthy",
  "timestamp": "2026-04-29T15:30:00Z"
}
```

---

### Extender el Agente

#### 1. Agregar nueva regla de validación

En `MockReglaValidacionRepository.GetReglasByProcesoAsync()`:
```csharp
reglas.Add(new ReglaValidacion
{
    Id = 7,
    ProcesoId = 1,
    Descripcion = "Nueva regla...",
    TipoObligatorioOpcional = "Obligatorio",
    CriterioEvaluacion = "Explicación técnica...",
    Activa = true
});
```

#### 2. Agregar datos mock adicionales

En `MockMcpClient`:
```csharp
// Agregar tarea
tareasMock.Add(new TrelloTaskDto { ... });

// Agregar registro
registrosMock.Add(new ClockifyRecordDto { ... });
```

#### 3. Modificar la estructura del prompt

En `PromptConstructor.ConstruirPrompt()`:
```csharp
sb.AppendLine("### NUEVA SECCIÓN");
sb.AppendLine("Instrucciones aquí...");
```

---

### Diagnóstico

#### Problema: API no inicia
```bash
# Verificar que OpenAI key está configurada
$env:OPENAI_API_KEY

# Si está vacía:
$env:OPENAI_API_KEY="sk-..."

# Reintenta
dotnet run
```

#### Problema: Tests fallan
```bash
# Ver output detallado
dotnet test --logger "console;verbosity=detailed"

# Ejecutar solo tests de datos mock
dotnet test --filter "Mock"
```

#### Problema: Hallazgos vacíos
```
Posibles causas:
1. Datos mock no contienen inconsistencias esperadas
2. Prompt no está extrayendo JSON correctamente
3. Kernel de Semantic no está retornando respuesta
```

---

### Performance Tips

1. **Caché de reglas**: Las reglas se obtienen cada validación. Considerar caché con TTL.
2. **Batch processing**: Si hay muchos proyectos, procesar en paralelo con TaskScheduler.
3. **LLM timeout**: Aumentar timeout en appsettings si OpenAI es lento.

---

### Migración a Producción

```bash
# 1. Reemplazar MockMcpClient con implementación real
# → Implementar comunicación real con MCP Server

# 2. Reemplazar MockReglaValidacionRepository con EF Core
# → Conectar a BD MySQL real

# 3. Cambiar OpenAI a Google Gemini (cuando esté disponible)
# → builder.Services.AddGoogleGenerativeAI("gemini-2.5-flash", key)

# 4. Agregar Swagger
# → builder.Services.AddSwaggerGen()
# → app.UseSwagger(), app.UseSwaggerUI()

# 5. Agregar Serilog
# → builder.Host.UseSerilog()

# 6. Docker
# → Crear Dockerfile
# → docker build -t agente-iso-validacion .
# → docker run -p 5000:5000 -e OPENAI_API_KEY=... agente-iso-validacion
```

---

### Notas Importantes

⚠️ **Guardrails del LLM**
- El prompt incluye: "NO CLASIFICAR hallazgos"
- El LLM retorna SOLO desvíos detectados
- La clasificación es responsabilidad del Agente de Clasificación

⚠️ **Deserialización robusta**
- El LLM puede incluir explicaciones junto al JSON
- `ExtraerJsonDeRespuesta()` busca los [ y ]
- Si no encuentra JSON válido, retorna []

⚠️ **Arquitectura sin acceso directo**
- NO hay HttpClient directo al código
- Todo pasa por interfaces inyectadas
- Facilita testing, cambio de implementación, seguridad

---

### Contacto / Soporte

- **Arquitecto Lead**: Desarrollador Senior .NET
- **Fecha Creación**: Abril 29, 2026
- **Stack**: .NET 9, Semantic Kernel, xUnit
- **Estado**: ✅ Funcional con datos mock
