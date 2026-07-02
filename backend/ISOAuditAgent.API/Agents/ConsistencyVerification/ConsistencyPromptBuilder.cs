// ============================================================================
//  ConsistencyPromptBuilder — Arma el prompt de turno de ConsistencyVerification
// ----------------------------------------------------------------------------
//  El prompt vive acá (carpeta del agente), no en el nodo del orquestador. El
//  nodo solo delega. Es la lógica de dominio del agente: qué se le pide al LLM
//  para evaluar secciones vacías según el propósito del documento.
//
//  EsCandidatoLlm define el conjunto de artefactos que el LLM ve (Exigibles +
//  Encontrados + con al menos una sección vacía). El nodo lo reutiliza en
//  ParsearRespuesta para validar la respuesta contra exactamente ese conjunto.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;

namespace ISOAuditAgent.API.Agents.ConsistencyVerification;

internal static class ConsistencyPromptBuilder
{
    /// <summary>
    /// Arma el prompt de turno. Si no hay artefactos con secciones vacías,
    /// devuelve el prompt de lista vacía.
    /// </summary>
    public static string Construir(DocumentosExtraidos input)
    {
        // El LLM recibe artefactos Exigibles + Encontrados que tengan al menos
        // una sección vacía (con o sin template). Incluye:
        //   - Artefactos SIN template.
        //   - Artefactos CON template: secciones presentes pero vacías (Caso 3).
        //     HallazgosEstructurales solo maneja Caso 1 (doc vacío) y Caso 2
        //     (sección ausente); el Caso 3 es ambiguo → LLM.
        var paraLlm = input.Artefactos
            .Where(EsCandidatoLlm)
            .ToList();

        if (paraLlm.Count == 0)
        {
            return "No hay secciones vacías para analizar. " +
                   "Respondé exactamente: {\"hallazgos\": []}";
        }

        var resumen = DocumentSummaryBuilder.Construir(paraLlm);

        return
            $"Sos un auditor experto en ISO 9001 y en el procedimiento {input.ProcedimientoCodigo} " +
            $"({input.ProcedimientoNombre}) de BDT Global.\n\n" +
            "Analizá los siguientes artefactos de un proyecto de desarrollo de software. " +
            "Para cada artefacto se indica el nombre, código, archivo encontrado, su " +
            "PROPOSITO según el procedimiento y el estado de sus secciones (LLENA o VACIA).\n\n" +
            "ARTEFACTOS A ANALIZAR:\n" +
            resumen + "\n\n" +
            "TU TAREA:\n" +
            "Para cada sección marcada como VACIA, evaluá si realmente es un hallazgo " +
            "auditable. NO toda sección vacía es un problema: depende del propósito del " +
            "documento y del rol de esa sección dentro de ese propósito.\n\n" +
            "CRITERIO PARA DECIDIR SI UNA SECCIÓN VACÍA ES HALLAZGO:\n" +
            "- Usá el campo 'Proposito' del artefacto como criterio principal: es para qué " +
            "sirve ese documento según el procedimiento de BDT. Una sección vacía ES " +
            "hallazgo cuando, sin esa sección, el documento NO cumple el propósito " +
            "declarado. Ejemplo: si el propósito de un Sign-Off es 'describir los " +
            "entregables del proyecto', entonces 'Entregables' vacío rompe ese propósito y " +
            "es hallazgo.\n" +
            "- NO es hallazgo si la sección es accesoria al propósito y puede estar " +
            "legítimamente vacía. Ejemplos típicos: 'Observaciones', 'Comentarios', " +
            "'Notas', 'Aclaraciones', 'Anexos', 'Historial de cambios' sin versiones previas.\n" +
            "- Si el artefacto no trae 'Proposito', juzgá por el nombre del documento y de " +
            "la sección con criterio de auditor.\n\n" +
            "QUÉ NO DEBÉS HACER:\n" +
            "- NO inventes hallazgos sobre contenido que no ves. Solo tenés títulos " +
            "de secciones y su estado. NO podés juzgar:\n" +
            "  * calidad del contenido;\n" +
            "  * consistencia entre documentos;\n" +
            "  * responsables o datos puntuales;\n" +
            "  * fechas o vigencia;\n" +
            "  * firmas (BDT no usa firmas digitales);\n" +
            "  * cruce con Trello o Clockify.\n" +
            "- NO emitas hallazgos sobre artefactos que no aparezcan en el resumen.\n" +
            "- Si ninguna sección vacía es realmente un hallazgo, respondé con lista vacía.\n\n" +
            "El campo 'artefactoEsperadoId' debe ser uno de los IDs listados arriba.\n" +
            "El campo 'origenRegla' debe ser exactamente 'Template'.\n\n" +
            "Respondé ÚNICAMENTE con este JSON (sin texto antes ni después, sin backticks):\n" +
            "{\n" +
            "  \"hallazgos\": [\n" +
            "    {\n" +
            "      \"artefactoEsperadoId\": <int>,\n" +
            "      \"descripcion\": \"Sección '<nombre de sección>' del documento está vacía\",\n" +
            "      \"justificacion\": \"<explicación de por qué esa sección vacía es un hallazgo en este tipo de documento>\",\n" +
            "      \"origenRegla\": \"Template\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";
    }

    /// <summary>
    /// Artefactos que el LLM de ConsistencyVerification ve: Exigibles +
    /// Encontrados + con al menos una sección detectada vacía. Excluye
    /// PendienteEtapaFutura, Faltante y NoBuscado (no hay nada que verificar).
    /// </summary>
    public static bool EsCandidatoLlm(ArtefactoExtraido a) =>
        a.Exigibilidad == ExigibilidadArtefacto.Exigible
        && a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado
        && a.SeccionesDetectadas.Any(s => !s.TieneContenido);
}
