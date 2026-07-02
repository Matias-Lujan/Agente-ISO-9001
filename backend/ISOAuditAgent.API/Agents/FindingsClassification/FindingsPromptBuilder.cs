// ============================================================================
//  FindingsPromptBuilder — Arma el prompt de turno de FindingsClassification
// ----------------------------------------------------------------------------
//  El prompt vive acá (carpeta del agente), no en el nodo del orquestador. El
//  nodo solo delega, pasándole los lotes de hallazgos y el contexto documental
//  (que el nodo cachea como estado de instancia). Es la lógica de dominio del
//  agente: cómo se le presentan los hallazgos al LLM y con qué reglas clasifica.
//
//  IDENTIDAD POR INDICE: los hallazgos se aplanan con indice estable 0..N-1.
//  El parser (ClasificacionResponseParser) replica EXACTAMENTE este aplanado
//  para mantener la correspondencia 1-a-1 (contrato 5).
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;

namespace ISOAuditAgent.API.Agents.FindingsClassification;

internal static class FindingsPromptBuilder
{
    /// <summary>
    /// Arma el prompt de clasificación. <paramref name="contexto"/> es el
    /// DocumentosExtraidos que el nodo conserva: aporta el propósito de cada
    /// artefacto y el procedimiento del proyecto.
    /// </summary>
    public static string Construir(
        IReadOnlyList<HallazgosPreliminares> hallazgos,
        DocumentosExtraidos contexto)
    {
        // Contexto por artefacto: nombre, archivo y propósito. Se usa para
        // enriquecer los hallazgos Template (sección ausente/vacía) y que el LLM
        // juzgue esencialidad y redacte la justificación contra el propósito.
        var contextoPorId = contexto.Artefactos
            .ToDictionary(
                a => a.ArtefactoEsperadoId,
                a => (
                    NombreArtefacto: a.NombreArtefacto,
                    NombreArchivo: a.DocumentoEncontrado?.NombreArchivo ?? string.Empty,
                    Proposito: a.Descripcion ?? string.Empty
                ));

        var preliminaresPlanos = hallazgos
            .SelectMany(lote => lote.Hallazgos)
            .Select((h, i) => (indice: i, hallazgo: h))
            .ToList();

        var lineas = preliminaresPlanos.Select(p =>
        {
            // Para hallazgos Template (sección ausente), agregar nombre del artefacto
            // y del archivo para que el LLM infiera el tipo de documento y genere
            // una justificación específica en lugar de la genérica del C#.
            var extra = string.Empty;
            if (p.hallazgo.OrigenRegla == OrigenRegla.Template
                && contextoPorId.TryGetValue(p.hallazgo.ArtefactoEsperadoId, out var ctx))
            {
                extra = $"\n  NombreArtefacto: {ctx.NombreArtefacto}";
                if (!string.IsNullOrEmpty(ctx.NombreArchivo))
                    extra += $"\n  ArchivoDocumento: {ctx.NombreArchivo}";
                if (!string.IsNullOrEmpty(ctx.Proposito))
                    extra += $"\n  PropositoDocumento: {ctx.Proposito}";
            }

            return
                $"indice: {p.indice}\n" +
                $"  artefactoEsperadoId: {p.hallazgo.ArtefactoEsperadoId}\n" +
                $"  Descripcion: {p.hallazgo.Descripcion}\n" +
                $"  Justificacion: {p.hallazgo.Justificacion}\n" +
                $"  OrigenRegla: {p.hallazgo.OrigenRegla}" +
                extra;
        });

        return
            $"Sos un auditor experto en ISO 9001 y en el procedimiento {contexto.ProcedimientoCodigo} " +
            $"({contexto.ProcedimientoNombre}) de BDT Global.\n\n" +
            "Clasificá cada hallazgo en NC, OBS u OM según las reglas de ese procedimiento.\n" +
            "Cada hallazgo viene identificado por un INDICE estable (no por " +
            "artefactoEsperadoId, porque puede haber múltiples hallazgos sobre el " +
            "mismo artefacto).\n" +
            $"Recibís {preliminaresPlanos.Count} hallazgos. Devolvé exactamente " +
            $"{preliminaresPlanos.Count} objetos, uno por cada indice.\n" +
            "No inventes, no omitas, no fusiones.\n\n" +
            "REGLAS DE CLASIFICACIÓN:\n" +
            "- NC (No Conformidad): incumplimiento directo de una exigencia del procedimiento.\n" +
            "- OBS (Observación): desviación menor respecto al template/estructura que NO impide que el documento cumpla su propósito.\n" +
            "- OM (Oportunidad de Mejora): posible mejora sin evidencia de incumplimiento.\n" +
            "- Si OrigenRegla es 'Tailoring': el tipo no puede ser NC (clasificar como OM en ese caso).\n" +
            "- Si OrigenRegla es 'Template': por defecto OBS. Es NC SOLO si la sección ausente/vacía es " +
            "ESENCIAL al propósito del documento (la que cumple su razón de ser, según PropositoDocumento). " +
            "Sección accesoria o de formato → OBS. Ante duda → OBS.\n\n" +
            "SOBRE LAS JUSTIFICACIONES:\n" +
            "- OrigenRegla=Procedimiento: usá la justificación del hallazgo tal como está, sin modificarla.\n" +
            "- OrigenRegla=Template (sección ausente del documento): la justificación recibida es " +
            "genérica. Reemplazala con una justificación específica y auditablemente útil que explique " +
            "POR QUÉ esa sección es esencial para el propósito del documento. Apoyate en " +
            "PropositoDocumento (para qué sirve el documento según el procedimiento de BDT) y en " +
            "NombreArtefacto. Conectá la sección ausente con el propósito que queda comprometido.\n" +
            "  Ejemplo bueno: 'El Sign-Off tiene por propósito describir los entregables del proyecto; " +
            "la ausencia de la sección Entregables impide cumplir ese propósito y verificar qué se " +
            "entregó efectivamente al cliente.'\n" +
            "  Ejemplo malo: 'La ausencia de una sección del template indica una desviación respecto " +
            "al formato estándar.'\n" +
            "- OrigenRegla=Tailoring: justificá concretamente la incoherencia detectada entre URL " +
            "del tailoring y archivo encontrado.\n\n" +
            "HALLAZGOS:\n\n" +
            string.Join("\n\n", lineas) +
            "\n\nSOLO el array JSON. Sin texto adicional. Sin backticks.\n" +
            "[{ \"indice\": <int>, \"tipo\": \"NC\"|\"OBS\"|\"OM\", " +
            "\"justificacion\": \"<justificación específica>\" }]";
    }
}
