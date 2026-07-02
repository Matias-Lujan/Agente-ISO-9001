// ============================================================================
//  CompliancePromptBuilder — Arma el prompt de turno de ComplianceValidation
// ----------------------------------------------------------------------------
//  El prompt vive acá (carpeta del agente), no en el nodo del orquestador. El
//  nodo (ComplianceValidationNode) solo delega: recibe el DTO, pide el prompt,
//  llama al LLM y parsea. Esta clase es la lógica de dominio del agente: qué se
//  le pide al LLM y sobre qué artefactos.
//
//  EsCandidatoLlm es público porque define el conjunto de artefactos que el LLM
//  ve, y el nodo lo reutiliza en ParsearRespuesta para validar la respuesta
//  contra exactamente ese mismo conjunto (deben coincidir sí o sí).
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

internal static class CompliancePromptBuilder
{
    /// <summary>
    /// Arma el prompt de turno para detectar incoherencias URL↔archivo sobre los
    /// artefactos candidatos. Si no hay candidatos, devuelve el prompt de lista
    /// vacía.
    /// </summary>
    public static string Construir(DocumentosExtraidos input)
    {
        var paraLlm = input.Artefactos
            .Where(EsCandidatoLlm)
            .ToList();

        if (paraLlm.Count == 0)
        {
            return "No hay artefactos para analizar. " +
                   "Respondé exactamente: {\"hallazgos\": []}";
        }

        var lineas = paraLlm.Select(a =>
            $"--- ARTEFACTO ID: {a.ArtefactoEsperadoId} ---\n" +
            $"  Nombre                  : {a.NombreArtefacto}\n" +
            $"  Codigo                  : {a.CodigoArtefacto ?? "(sin código formal)"}\n" +
            $"  UrlReferenciaTailoring  : {a.UrlReferencia}\n" +
            $"  NombreArchivo encontrado: {a.DocumentoEncontrado!.NombreArchivo}\n");

        return
            $"Procedimiento que rige a este proyecto: {input.ProcedimientoCodigo} " +
            $"({input.ProcedimientoNombre}).\n\n" +
            "Analizá los siguientes artefactos para detectar INCOHERENCIAS EVIDENTES " +
            "entre la URL declarada en el tailoring y el archivo encontrado.\n\n" +
            "ARTEFACTOS A ANALIZAR:\n\n" +
            string.Join("\n", lineas) +
            "\nSi no hay incoherencias evidentes, respondé {\"hallazgos\": []}.\n" +
            "No inventes hallazgos sobre material que no tengas información para juzgar.\n\n" +
            "El campo 'artefactoEsperadoId' debe ser uno de los IDs listados arriba.\n" +
            "El campo 'origenRegla' debe ser exactamente 'Tailoring'.\n\n" +
            "Respondé ÚNICAMENTE con este JSON, sin texto adicional ni backticks:\n" +
            "{\n" +
            "  \"hallazgos\": [\n" +
            "    {\n" +
            "      \"artefactoEsperadoId\": <int>,\n" +
            "      \"descripcion\": \"Incoherencia entre URL del tailoring y archivo encontrado\",\n" +
            "      \"justificacion\": \"El tailoring declara URL '<url>' pero se encontró '<archivo>', que no corresponde al artefacto.\",\n" +
            "      \"origenRegla\": \"Tailoring\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";
    }

    /// <summary>
    /// Artefactos que el LLM de ComplianceValidation ve: exigibles, que aplican,
    /// encontrados en Drive, con URL de referencia (no de carpeta) y nombre de
    /// archivo. Trello/Clockify no tienen URL↔archivo comparable.
    /// </summary>
    public static bool EsCandidatoLlm(ArtefactoExtraido a) =>
        a.Exigibilidad == ExigibilidadArtefacto.Exigible
        && a.EstadoAplicacionTailoring == EstadoAplicacionTailoring.Aplica
        && a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado
        && a.DocumentoEncontrado?.Fuente == FuenteDocumento.Drive  // Trello/Clockify no tienen URL↔archivo comparable
        && !string.IsNullOrWhiteSpace(a.UrlReferencia)
        && !EsFolderUrlDrive(a.UrlReferencia!)
        && a.DocumentoEncontrado is not null
        && !string.IsNullOrWhiteSpace(a.DocumentoEncontrado.NombreArchivo);

    // URL de carpeta Drive (/folders/): carpeta→archivo encontrado dentro es
    // el patrón esperado; el LLM no puede juzgar coherencia en ese caso.
    private static bool EsFolderUrlDrive(string url) =>
        url.Contains("/folders/", StringComparison.OrdinalIgnoreCase);
}
