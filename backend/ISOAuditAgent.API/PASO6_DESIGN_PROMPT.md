## PASO 6: Diseño del Prompt - El Núcleo del Agente

### Estado actual
✅ **Compilación exitosa** - El proyecto compila correctamente con el prompt dinámico

### Archivos creados/modificados

#### 1. **PromptConstructor.cs** (Nuevo)
Clase responsable de construir el prompt dinámico que se envía al LLM.

**Estructura del Prompt:**

```
┌─ SECCIÓN 1: Instrucciones Generales
│  └─ Explica la tarea al LLM
│
├─ SECCIÓN 2: GUARDRAILS CRÍTICOS ⚠️
│  ├─ No clasificar hallazgos (NO es responsabilidad de este agente)
│  ├─ Retornar estrictamente JSON
│  └─ Basarse en datos objetivos
│
├─ SECCIÓN 3: Reglas de Validación
│  ├─ Reglas OBLIGATORIAS (Strategy Pattern)
│  └─ Reglas OPCIONALES (Strategy Pattern)
│
├─ SECCIÓN 4: Datos de Trello (JSON)
│  └─ Array con todas las tareas planificadas
│
├─ SECCIÓN 5: Datos de Clockify (JSON)
│  └─ Array con todos los registros de tiempo
│
├─ SECCIÓN 6: Instrucciones de Análisis Específico
│  ├─ Registros Huérfanos
│  ├─ Discrepancia de Responsables
│  ├─ Registros Fuera de Plazo
│  ├─ Tareas sin Registros
│  ├─ Incoherencias en Horas
│  └─ Registros Recientes
│
├─ SECCIÓN 7: Estructura JSON de Salida
│  └─ Especifica exactamente cómo retornar los hallazgos
│
├─ SECCIÓN 8: Ejemplo Concreto
│  └─ Muestra un hallazgo bien formado
│
└─ SECCIÓN 9: Contexto Final
   └─ Metadatos del análisis
```

### Flujo de Ejecución

```
ComplianceValidationAgent.ValidateProcessAsync()
  │
  ├─ Obtener tareas de Trello (vía MCP)
  ├─ Obtener registros de Clockify (vía MCP)
  ├─ Obtener reglas de validación (vía Repository)
  │
  ├─ Agrupar reglas (Strategy Pattern):
  │  ├─ Reglas OBLIGATORIAS (siempre validar)
  │  └─ Reglas OPCIONALES (validar condicionalmente)
  │
  ├─ Crear ContextoValidacion con todos los datos
  │
  └─ AnalizarConLLMAsync(contexto)
      │
      ├─ PromptConstructor.ConstruirPrompt()
      │  └─ Genera prompt dinámico con:
      │     - Guardrails explícitos
      │     - Reglas del proceso
      │     - Datos de Trello/Clockify en JSON
      │     - Instrucciones de análisis
      │     - Estructura de salida esperada
      │
      ├─ kernel.InvokePromptAsync(prompt)
      │  └─ Envía prompt a OpenAI/Gemini
      │
      ├─ ExtraerJsonDeRespuesta(respuesta)
      │  └─ Busca array JSON [ ... ] en respuesta
      │
      ├─ DeserializarHallazgos(json)
      │  └─ Mapea JSON a ValidationFinding
      │
      └─ Retorna List<ValidationFinding>
```

### Guardrails Implementados

#### 1. **NO CLASIFICAR** (Crítico)
```
El agente SOLO detecta desvíos.
NO debe decir: "Esto es una No Conformidad"
El Agente de Clasificación es responsable de eso.
```

#### 2. **Formato JSON Estricto**
```
El LLM DEBE retornar estrictamente:
[
  {
    "descripcion": "...",
    "fuente": "Trello vs Clockify",
    "idTareaRelacionada": "TRE-001",
    "reglaIncumplida": "...",
    "detallesJSON": "{...}",
    "severidad": "Alta|Media|Baja"
  }
]

Si no hay inconsistencias: []
```

#### 3. **Datos Objetivos**
```
- Comparación de IDs
- Comparación de fechas
- Comparación de responsables/usuarios
- Presencia/ausencia de registros
```

### Ejemplo de Hallazgo Detectado

**Entrada (Datos Trello + Clockify):**
```
Trello:
{
  "id": "TRE-001",
  "responsable": "Juan Pérez",
  "estado": "En Progreso"
}

Clockify:
{
  "idTarea": "TRE-001",
  "usuario": "Carlos López",  // ← DISTINTO
  "horasRegistradas": 2.5
}
```

**Salida (Hallazgo JSON):**
```json
{
  "descripcion": "La tarea TRE-001 está asignada a 'Juan Pérez' en Trello, pero en Clockify hay registros de 'Carlos López' para la misma tarea.",
  "fuente": "Trello vs Clockify",
  "idTareaRelacionada": "TRE-001",
  "reglaIncumplida": "El responsable en Trello debe ser coherente con quien registra tiempo en Clockify",
  "detallesJSON": "{\"responsableTrello\":\"Juan Pérez\",\"usuarioClockify\":\"Carlos López\",\"registroId\":\"CLO-007\"}",
  "severidad": "Media"
}
```

### Tipos de Inconsistencias que Detecta

| # | Tipo | Ejemplo | Severidad |
|---|------|---------|-----------|
| 1 | Registros Huérfanos | CLO-006 → TRE-999 (no existe) | Alta |
| 2 | Discrepancia Responsable | TRE-001: Juan vs CLO-007: Carlos | Media |
| 3 | Fuera de Plazo | Registro el 2024-01-20 pero vencimiento 2024-01-15 | Alta |
| 4 | Sin Registros | Tarea completada sin evidencia de trabajo | Alta |
| 5 | Horas Excesivas | Suma > 80 horas para una tarea | Media |
| 6 | Inactividad Sospechosa | Tarea "En Progreso" sin registros recientes | Baja |

### Métodos de Soporte

#### `ExtraerJsonDeRespuesta(respuesta)`
```csharp
// El LLM puede incluir explicaciones:
// "Aquí están los hallazgos: [{ ... }] Fin del análisis"
// 
// Este método extrae solo el JSON válido

string json = ExtraerJsonDeRespuesta(respuestaDelLLM);
// Resultado: "[{ ... }]"
```

#### `DeserializarHallazgos(json)`
```csharp
// Mapea el JSON de la respuesta a objetos C#
// Maneja campos faltantes gracefully

List<ValidationFinding> hallazgos = DeserializarHallazgos(json);
// Resultado: List<ValidationFinding> con todos los hallazgos
```

### Configuración en appsettings.json

```json
{
  "ComplianceValidation": {
    "Timeout": 30000,      // Timeout del análisis (ms)
    "MaxRetries": 3        // Reintentos en caso de error
  }
}
```

### Próximo paso: PASO 7

En el PASO 7 implementaremos:
1. **Manejo de errores robusto** para timeouts y excepciones
2. **Logging detallado** de cada paso del análisis
3. **Pruebas unitarias** del agente con datos mock
4. **Endpoint HTTP** para invocar el agente desde clientes

### ✅ Compilación exitosa
```
dotnet build
// Resultado: Compilación realizado correctamente ✓
```

---

**RESUMEN PASO 6:**
- ✅ Creada clase `PromptConstructor` que genera prompts dinámicos
- ✅ Implementado método `AnalizarConLLMAsync()` que invoca el LLM
- ✅ Creado método `ExtraerJsonDeRespuesta()` para parsear respuestas
- ✅ Creado método `DeserializarHallazgos()` para mapear JSON a objetos
- ✅ Guardrails implementados: No clasificar, formato JSON, datos objetivos
- ✅ Proyecto compila sin errores
