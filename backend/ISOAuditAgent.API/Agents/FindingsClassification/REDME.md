# Agente ISO 9001 — Agente de Clasificación de Hallazgos

**FindingsClassification — función de cada archivo y decisiones de diseño**

Cuarto agente del pipeline de auditoría de **BDT Global**. Recibe los hallazgos que detectaron los agentes anteriores y les asigna una gravedad: No Conformidad, Observación u Oportunidad de Mejora. Este documento describe qué hace cada archivo del agente, cómo se conecta con el resto del workflow y por qué se tomó cada decisión.

**Stack:** .NET 9 (C#) · Microsoft Agent Framework (MAF) · Gemini 2.5 Flash como modelo de lenguaje. El agente es un nodo dentro de un grafo de workflow de MAF.

---

## 1. Qué hace el agente

El sistema audita proyectos de software con un **pipeline de cuatro agentes** que se ejecutan en orden dentro de un grafo de MAF. El agente de clasificación es el **cuarto y último agente de IA** del recorrido. 

- **Clasifica, no detecta.** Los agentes previos (ComplianceValidation y ConsistencyVerification) ya detectaron los problemas. Este agente **solo decide la gravedad** de cada uno. No busca problemas nuevos.
- **Correspondencia 1 a 1.** Por cada hallazgo preliminar que entra, sale exactamente un hallazgo clasificado. 
- **Valida contra el procedimiento del proyecto, no contra la ISO 9001 directamente.** La ISO no dice cómo hacer las cosas; dice que se haga lo que el procedimiento interno define. El agente razona siempre contra el procedimiento del proyecto (PR 11-13 u otro).

Las tres gravedades posibles:

| Tipo | Significado |
|---|---|
| `NC` | No Conformidad: incumplimiento directo y verificable de un requisito del procedimiento (un artefacto obligatorio ausente o que no cumple su función). Degrada el artefacto a "No conforme". |
| `OBS` | Observación: desvío menor que no implica incumplimiento directo (riesgo, inconsistencia formal, sección no esencial vacía). |
| `OM` | Oportunidad de Mejora: sugerencia que no incumple nada escrito en el procedimiento (buena práctica). |

---

## 2. Dónde encaja en el pipeline

El grafo de agentes se recorre así:

```
[1] ResolutorContexto → [2] DocumentAnalysis → [3] ComplianceValidation
                                            ↘ [4] ConsistencyVerification

  [3] y [4] → [5] FindingsClassification → [6] ConsolidadorResultado → salida
```

El agente de clasificación **[5]** recibe **tres entradas** que llegan por caminos distintos y en momentos distintos:

- **El contexto de documentos** (`DocumentosExtraidos`), por una arista directa desde DocumentAnalysis [2].
- **Los hallazgos de ComplianceValidation** [3].
- **Los hallazgos de ConsistencyVerification** [4].

Los dos lotes de hallazgos llegan por un **fan-in barrier** (MAF junta dos ramas que corrían en paralelo). El contexto viaja por un **carril aparte**: el agente lo conserva sin que el LLM lo toque y se lo pasa al consolidador. Una vez que tiene las tres cosas, dispara la clasificación.

---

## 3. Entradas y salidas (los contratos)

Los contratos son los **tipos de datos** que el agente consume y produce. Viven en `Agents/Contracts/` y son compartidos con los demás agentes, lo que garantiza que las piezas encajen.

### Entrada — `HallazgosPreliminares`

Lo producen los validadores. Cada lote trae el `AuditoriaId`, el `AgenteOrigen` (quién lo generó) y una lista de `HallazgoPreliminar`. Cada hallazgo preliminar tiene:

| Campo | Para qué sirve |
|---|---|
| `ArtefactoEsperadoId` | A qué artefacto del proyecto se refiere el hallazgo. |
| `Descripcion` | Qué se detectó. |
| `Justificacion` | Por qué es un problema y qué regla se incumple. |
| `OrigenRegla` | De dónde sale la regla incumplida: `Procedimiento`, `Template` o `Tailoring`. Es la clave para decidir la gravedad máxima posible. |

### Salida — `HallazgosClasificados`

Por cada preliminar, un `HallazgoClasificado` con su **Tipo** (NC/OBS/OM), la descripción, una justificación y el agente que lo originó. El agente envuelve esto junto al contexto en un `ResultadoClasificacionConContexto` — la clasificación (del LLM) más el `DocumentosExtraidos` original (que el LLM nunca tocó). Ese wrapper es la única entrada del consolidador.

---

## 4. Archivos del agente

La lógica del agente se reparte entre su carpeta propia y el orquestador. Lo separamos así porque el **nodo** (mecánica de MAF: cacheo, reintentos, ruteo) es responsabilidad del orquestador, mientras que el **prompt** y el **parseo** son propios del agente.

### Carpeta `Agents/FindingsClassification/`

| Archivo | Función |
|---|---|
| `SystemPrompts.cs` | Las instrucciones del clasificador en lenguaje natural: qué significa cada gravedad, la "regla de oro" por `OrigenRegla`, cómo tratar los hallazgos de template y cómo devolver la respuesta (solo un array JSON). Es el "manual del auditor" que se le da al LLM. Documenta además qué reglas del diseño original se eliminaron (asumían cruces de datos que este agente no ve). |
| `ClasificacionResponseParser.cs` | Convierte la respuesta JSON del LLM en datos tipados y, sobre todo, hace cumplir las invariantes: que la cantidad coincida, que cada índice exista una sola vez y que ninguno quede fuera de rango. Si algo no cierra, lanza una excepción (falla ruidosa). También fuerza la regla de oro: un hallazgo de Tailoring nunca puede quedar como NC. |

### En el orquestador (`Agents/Orchestrator/`)

| Archivo | Función |
|---|---|
| `Nodes/NodosWorkflow.cs` | Contiene la clase `FindingsClassificationNode`: el nodo de MAF que ejecuta el agente. Cachea las tres entradas, arma el prompt con índice estable, llama al LLM con reintentos, invoca al parser y emite el resultado. Reporta su progreso (en curso / completado / fallido) al tracker. |
| `Workflow/AuditoriaWorkflowFactory.cs` | Arma el grafo: conecta la arista directa de DocumentAnalysis y el fan-in barrier de los dos validadores hacia el nodo de clasificación, y la salida del nodo hacia el consolidador. |
| `Integrations/LLM/GeminiServiceCollectionExtensions.cs` | Registra el agente de IA (un `AIAgent` de Gemini por nodo) en el contenedor de dependencias, con su modelo y configuración. |

**Contratos relacionados:** `Contracts/HallazgosPreliminares.cs` (entrada), `Contracts/HallazgosClasificados.cs` (salida) y `Models/TipoHallazgo.cs` (el enum NC/OBS/OM).

---

## 5. Cómo funciona, paso a paso

1. **Cachea las tres entradas.** Como los mensajes pueden llegar de a uno y en cualquier orden, el nodo guarda cada uno en su lugar y distingue los dos lotes de hallazgos por su `AgenteOrigen`. Recién cuando tiene los tres, arranca.
2. **Verifica que sean de la misma auditoría.** Si los IDs no coinciden, falla: es una señal de que algo se cableó mal.
3. **Aplana los hallazgos con un índice estable.** Junta los dos lotes en una sola lista y le pone a cada hallazgo un número 0, 1, 2… Ese índice —no el artefacto— es la identidad de cada hallazgo para el LLM.
4. **Arma el prompt.** Le indica al LLM el procedimiento del proyecto, las reglas de clasificación y la lista numerada de hallazgos. Para los de template, le suma el propósito del documento para que escriba una justificación específica y no genérica.
5. **Llama al LLM con reintentos.** El modelo a veces devuelve JSON malformado; un reintento inmediato (hasta 3) suele resolverlo sin cambiar el prompt.
6. **Parsea y valida 1 a 1.** El parser exige que la respuesta tenga exactamente la misma cantidad de objetos que de hallazgos, con cada índice presente una sola vez.
7. **Aplica la regla de oro determinística.** Aunque el prompt ya se la pidió al LLM, el código vuelve a forzar que un hallazgo de Tailoring no quede como NC. Doble red de seguridad.
8. **Pega el contexto y emite el resultado.** Combina la clasificación con el contexto de documentos conservado y lo manda al consolidador. Por las dudas, libera el estado cacheado.

---

## 6. Decisiones de diseño 

Las decisiones más importantes y por qué se tomaron:

- **Identidad por índice estable, no por artefacto.** Un mismo artefacto puede tener varios hallazgos (de distintos agentes y reglas). Si se identificara por artefacto, se confundirían. El índice garantiza la correspondencia 1 a 1 exacta.
- **Fallar ruidoso ante desalineación.** Si el LLM devuelve de más, de menos o con índices repetidos, el parser lanza una excepción en vez de devolver datos incompletos. Tapar el error escondería bugs reales del modelo.
- **Doble red de seguridad para la regla de oro.** La regla "Tailoring nunca es NC" está enunciada en el prompt (para que el LLM la aplique) y forzada en el código (por si el LLM la pasa por alto). No se confía ciegamente en el modelo.
- **Cacheo robusto ante el comportamiento del barrier.** No se asume cómo agrupa MAF los mensajes del fan-in: se toma el caso conservador de que llegan de a uno y se los cachea. Funciona en cualquier caso.
- **Estado por ejecución, nunca Singleton.** El nodo guarda estado (las tres entradas), así que se crea uno nuevo por cada auditoría. Compartirlo entre auditorías mezclaría datos.
- **El contexto no pasa por el LLM.** El `DocumentosExtraidos` viaja por un carril aparte y el agente solo lo transporta. Esto evita que el LLM lo altere y mantiene al consolidador como un paso simple y sin estado.
- **Reglas depuradas respecto del diseño original.** El agente clasifica hallazgos preliminares, no documentos.