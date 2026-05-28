namespace ISOAuditAgent.API.Models;

// Nodos del workflow que el frontend muestra como "agentes IA en ejecución".
// Se excluyen los nodos deterministas (ResolutorContexto, ConsolidadorResultado)
// porque no son agentes LLM y no aportan información útil al usuario final.
public enum NodoWorkflow
{
    DocumentAnalysis,
    ComplianceValidation,
    ConsistencyVerification,
    FindingsClassification
}
