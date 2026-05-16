# Agente de Clasificación de Hallazgos ISO 9001

**Proyecto:** Sistema de Auditoría Inteligente — BDT Global
**Procedimiento:** PR 11-13 — Diseño, Análisis, Desarrollo e Implementación de Software
**Framework:** Microsoft Agent Framework (MAF) v1.1
**LLM:** Gemini 2.5 Flash (Google AI Studio)
**Versión contratos:** v2.1

---

## El problema que resuelve

BDT Global audita manualmente sus proyectos de software contra el procedimiento interno PR 11-13. Un auditor revisa Drive, Trello y Clockify, compara contra el procedimiento y decide si algo es una No Conformidad, una Observación o una Oportunidad de Mejora. Es lento, subjetivo y propenso a errores.

Este sistema automatiza esa auditoría usando inteligencia artificial.

---

## Arquitectura general

```
Auditor (usuario)
      ↓  POST /api/auditorias  (proyecto + etapa)
API REST (.NET)
      ↓  dispara el workflow
Workflow MAF (4 agentes con LLM + 1 nodo determinista)
      ↓  produce AuditoriaResultado
MySQL
      ↓  persiste en transacción única
Dashboard (ReactJS)
```

---

## El workflow MAF — los 5 nodos

```
IniciarAuditoriaWorkflowInput
          ↓
    DocumentAnalysis          ← agente con LLM
          ↓
    DocumentosExtraidos       ← Contrato 2
          ↓
    ┌─────┴─────┐
    ↓           ↓
ComplianceV  ConsistencyV     ← en paralelo, cada uno con LLM
    ↓           ↓
Hallazgos   Hallazgos         ← Contrato 3 (uno por agente)
    └─────┬─────┘
          ↓  fan-in barrier
  FindingsClassification      ← ESTE AGENTE
          ↓
  HallazgosClasificados       ← Contrato 4
          ↓
  ConsolidadorResultado  ←─── DocumentosExtraidos
          ↓                   (sin LLM, solo código)
  AuditoriaResultado          ← Contrato 5
          ↓
  API REST → MySQL
```

### Nodo 1 — DocumentAnalysis
Lee el Tailoring (FR 29) y sale a buscar cada artefacto esperado según el PR 11-13 y la etapa indicada por el auditor. Para cada artefacto determina exigibilidad, estado en el Tailoring, si existe en Drive y sus secciones detectadas.

### Nodos 2a y 2b — ComplianceValidation y ConsistencyVerification
Corren en paralelo sobre el mismo DocumentosExtraidos.

- **ComplianceValidation** detecta desvíos entre lo planificado y lo ejecutado.
- **ConsistencyVerification** detecta inconsistencias entre fuentes (Drive vs Trello vs Tailoring).

### Nodo 3 — FindingsClassification (ESTE AGENTE)
Recibe los dos lotes de HallazgosPreliminares y los clasifica formalmente en NC, OBS u OM.

### Nodo 4 — ConsolidadorResultado
Nodo determinista sin LLM. Ensambla el AuditoriaResultado final con las tres listas que mapean a las tablas de MySQL.

---

## Este agente — FindingsClassification

### Posición en el workflow

```
ComplianceValidation  ──┐
                        ├─ [fan-in] ──► FindingsClassification ──► ConsolidadorResultado
ConsistencyVerification ┘
```

### Responsabilidad única
Recibe hallazgos preliminares de los dos validadores y les asigna clasificación formal. No inventa, no omite, no fusiona. Relación 1 a 1 estricta.

### Contratos

**Input — Contrato 3: HallazgosPreliminares**
```
HallazgosPreliminares(
    AuditoriaId,
    AgenteOrigen,        ← en el raíz, no en cada hallazgo
    Hallazgos: [
        HallazgoPreliminar(
            ArtefactoEsperadoId,   ← llave de unión
            Descripcion,
            Justificacion,
            OrigenRegla            ← Procedimiento | Template | Tailoring
        )
    ]
)
```

**Output — Contrato 4: HallazgosClasificados**
```
HallazgosClasificados(
    AuditoriaId,
    Hallazgos: [
        HallazgoClasificado(
            ArtefactoEsperadoId,
            Tipo,            ← NC | OBS | OM
            Descripcion,
            Justificacion,
            AgenteOrigen     ← propagado del preliminar
        )
    ]
)
```

### Flujo interno

```
1. Primera llamada → ProcesarAsync(hallazgosCompliance)
   └─ acumula → Count=1 → devuelve null (espera el segundo)

2. Segunda llamada → ProcesarAsync(hallazgosConsistency)
   └─ acumula → Count=2 → procesa todo junto

3. ClasificarAsync()
   └─ construye diccionario ArtefactoEsperadoId → (Hallazgo, AgenteOrigen)
   └─ ArmarPrompt() → manda hallazgos a Gemini
   └─ agent.RunAsync(prompt) → Gemini clasifica
   └─ agentResponse.Text → JSON con NC/OBS/OM por artefactoId

4. ClasificacionResponseParser.Parsear()
   └─ limpia backticks del JSON
   └─ recupera hallazgo original por ArtefactoEsperadoId
   └─ aplica regla: si OrigenRegla != Procedimiento → máximo OM
   └─ devuelve HallazgosClasificados

5. YieldOutputAsync(resultado)
   └─ MAF enruta al ConsolidadorResultado
```

---

## Estructura de archivos

```
AgenteAuditoria/
├── README.md                              ← este archivo
├── AgenteAuditoria.csproj
├── Program.cs                             ← prueba aislada con datos del proyecto 30052
├── Models/
│   ├── Enums.cs                           → TipoHallazgo, OrigenRegla, AgenteOrigen
│   ├── Contrato3_HallazgosPreliminares.cs → INPUT del agente
│   └── Contrato4_HallazgosClasificados.cs → OUTPUT del agente
├── Services/
│   └── ClasificacionResponseParser.cs     → convierte JSON de Gemini a objetos C#
├── Agents/
│   └── AgentFactory.cs                    → ChatClientAgent con reglas PR 11-13
└── Executors/
    └── FindingsClassificationExecutor.cs  → fan-in + clasificación
```

---

## Reglas de clasificación — PR 11-13

### No Conformidades (NC) — incumplimiento directo del procedimiento

| Código | Regla |
|--------|-------|
| NC-01 | Clockify sin horas en período con tareas completadas en Trello |
| NC-02 | Artefacto Aplica=Sí en Tailoring pero no existe en Drive |
| NC-03 | FR 25 faltante cuando hay paquetes en el repositorio |
| NC-04 | Código no versionado en SVN/GIT/Bitbucket |
| NC-05 | FR 48 sin firma del cliente en proyecto en implementación |
| NC-06 | Tailoring desactualizado respecto al estado real |
| NC-07 | Artefacto excluido del Tailoring sin justificación documentada |

### Observaciones (OBS) — riesgo potencial sin evidencia directa

| Código | Regla |
|--------|-------|
| OBS-01 | Cronograma sin nueva versión en más de 2 semanas |
| OBS-02 | Trello no refleja el avance real del proyecto |
| OBS-03 | Horas en Clockify superan el estimado sin actualización |
| OBS-04 | Artefacto Evaluar&Justificar sin justificación en Tailoring |
| OBS-05 | FR 11 faltante para reuniones importantes |
| OBS-06 | FR 71 sin actualizar en proyecto activo |

### Oportunidades de Mejora (OM) — sugerencia sin incumplimiento

| Código | Regla |
|--------|-------|
| OM-01 | Inconsistencia de responsables entre documentos |
| OM-02 | Estructura de carpetas de Drive incompleta |
| OM-03 | Práctica que existe pero no está en el procedimiento |

---

## La regla de negocio más importante

```csharp
// Si la regla no viene del Procedimiento → máximo OM, nunca NC
if (origenRegla != OrigenRegla.Procedimiento && tipoParseado == TipoHallazgo.NC)
    return TipoHallazgo.OM;
```

Solo el PR 11-13 puede generar No Conformidades. Si la regla incumplida viene del
Template o del Tailoring, lo más severo posible es Oportunidad de Mejora.

---

## Principios de diseño

**1. Validar contra PR 11-13, no contra la ISO**
La ISO 9001 no dice cómo hacer las cosas — dice que se haga lo que el proceso dice.
El proceso de BDT es el PR 11-13.

**2. La etapa es un input humano**
La IA no puede deducir en qué etapa está el proyecto mirando los documentos.
Un proyecto puede llegar al cierre con documentación de kick-off.
El auditor lo indica explícitamente antes de ejecutar el workflow.

**3. Existencia, secciones y templates — no contenido semántico**
El auditor real no analiza si los requerimientos son correctos.
Verifica que el documento existe, tiene las secciones del template
y coincide con lo declarado en el Tailoring.

**4. Un hallazgo entra, un hallazgo sale**
FindingsClassification no inventa, omite ni fusiona. Relación 1 a 1 estricta.

**5. PR 11-13 en system prompt — ahorro de tokens**
Las reglas del procedimiento viven una sola vez en el system prompt.
No se repiten en cada llamada. El payload de cada clasificación
es solo los hallazgos — liviano.

**6. ConsolidadorResultado sin LLM**
Ensamblar listas no requiere razonamiento. Es código determinista.
Mezclar eso con un LLM gastaría tokens innecesariamente.

---

## Instalación y ejecución

```bash
# 1. Instalar paquetes
dotnet add package Microsoft.Agents.AI --version 1.1.0
dotnet add package Microsoft.Agents.AI.Workflows --version 1.1.0
dotnet add package Microsoft.Extensions.AI --version 10.4.0
dotnet add package Mscc.GenerativeAI.Microsoft --version 3.1.0
dotnet add package Microsoft.Extensions.Configuration --version 10.0.3
dotnet add package Microsoft.Extensions.Configuration.UserSecrets --version 10.0.3

# 2. Configurar API key de Gemini
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "TU_KEY"
# Obtener key en: aistudio.google.com → Get API key

# 3. Compilar y ejecutar
dotnet build
dotnet run
# El sistema pregunta la etapa del proyecto (1-6) antes de clasificar
```

---

## Ejemplo de salida por consola

```
=================================================
 AGENTE DE CLASIFICACIÓN DE HALLAZGOS ISO 9001
 PR 11-13 — BDT Global
=================================================
 Etapa actual del proyecto (1-6): 3
 Etapa: Desarrollo

[FindingsClassification] Clasificando 14 hallazgos
(ComplianceValidation: 8 + ConsistencyVerification: 6)...
[FindingsClassification] ✓ Gemini clasificó los hallazgos.
  [INFO] NC degradado a OM: OrigenRegla=Tailoring
  [INFO] NC degradado a OM: OrigenRegla=Template

=================================================
 RESULTADO — Auditoría 30052
=================================================
  NC:  3
  OBS: 5
  OM:  6
  Total: 14
=================================================

[NC ] Clockify sin horas registradas en período con tareas completadas
       Artefacto ID: 101
       Agente:       ComplianceValidation
       Justificación: NC-01: Clockify sin horas en período con tareas completadas.

[OM ] FR 48 Sign-Off sin firma del cliente
       Artefacto ID: 108
       Agente:       ComplianceValidation
       Justificación: NC-05 (degradado a OM por OrigenRegla=Template)

✓ HallazgosClasificados listo para el ConsolidadorResultado.
```

---

## Tipos de proyecto (FR 29 — Tailoring)

| Tipo | Horas | Implicancia |
|------|-------|-------------|
| **A** | > 1200 hs | Todos los artefactos son Mandatorios |
| **B** | ≤ 1200 hs | Algunos son Evaluar & Justificar |

En tipo B: artefacto faltante CON justificación en Tailoring → no es NC.
Sin justificación → NC-07.