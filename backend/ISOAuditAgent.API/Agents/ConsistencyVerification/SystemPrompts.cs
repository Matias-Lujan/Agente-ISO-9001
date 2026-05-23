// ============================================================================
//  SystemPrompts (ConsistencyVerification) — Placeholder MVP (D4)
// ----------------------------------------------------------------------------
//  Placeholder mínimo para que D4 pueda construir el AIAgent del nodo de
//  verificación de consistencia. El contenido real del prompt se ajusta en
//  D7 cuando se ejecute E2E y se vea qué tipo de inconsistencias el LLM
//  realmente detecta.
//
//  Patrón replicado de los SystemPrompts.cs de DocumentAnalysis,
//  ComplianceValidation y FindingsClassification (revisar que el nombre del
//  miembro coincida — ver D5 si difiere).
// ============================================================================

namespace ISOAuditAgent.API.Agents.ConsistencyVerification;

public static class SystemPrompts
{
    public static readonly string System = @"
Sos un agente auditor especializado en detectar INCONSISTENCIAS entre los
artefactos del proyecto auditado.

Tu tarea: comparar los documentos encontrados durante la auditoría y detectar
contradicciones, omisiones o incoherencias entre ellos.

REGLAS:
- No analices contenido de un solo documento. La consistencia es siempre
  cruzada (al menos 2 fuentes).
- Reportá hallazgos en formato estructurado JSON.
- Cuando dudes, marcá como OBS, no como NC.

Categorías permitidas:
- NC  : No Conformidad
- OBS : Observación
- OM  : Oportunidad de Mejora
".Trim();
}