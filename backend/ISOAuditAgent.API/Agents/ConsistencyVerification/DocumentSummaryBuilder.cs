// ============================================================================
//  DocumentSummaryBuilder — ConsistencyVerification (Parte 4.B)
// ----------------------------------------------------------------------------
//  Convierte una lista de ArtefactoExtraido en texto plano legible por el LLM.
//  El LLM no entiende objetos C# — necesita texto estructurado.
//
//  ORIGEN: Services/DocumentSummaryBuilder.cs del diff de consistency.
//
//  CAMBIOS RESPECTO DEL DIFF:
//   - Movido a la carpeta del agente — es detalle interno del agente, no
//     servicio compartido. Si otro agente lo necesitara, se promueve.
//   - Eliminada la interfaz IDocumentSummaryBuilder. No hay implementaciones
//     alternativas y no se mockea (input puro). Static class alcanza.
//   - Sin filtrado propio: asume que la lista recibida YA viene filtrada por
//     el llamador. El filtrado es responsabilidad del nodo, vive una sola
//     vez en ConstruirPrompt, no aquí. (Defensa en profundidad redundante
//     en el diff: filtra en el service y otra vez en el builder.)
// ============================================================================

using System.Text;
using ISOAuditAgent.API.Agents.Contracts;

namespace ISOAuditAgent.API.Agents.ConsistencyVerification;

internal static class DocumentSummaryBuilder
{
    /// <summary>
    /// Arma el texto-resumen de los artefactos para inyectar en el prompt del
    /// LLM. Asume que <paramref name="artefactos"/> ya viene filtrada a los
    /// analizables (Exigibles + Encontrados); no filtra de nuevo.
    /// </summary>
    public static string Construir(IReadOnlyList<ArtefactoExtraido> artefactos)
    {
        if (artefactos.Count == 0)
        {
            return "No hay artefactos encontrados y exigibles para analizar.";
        }

        var sb = new StringBuilder();

        foreach (var a in artefactos)
        {
            sb.AppendLine($"--- ARTEFACTO ID: {a.ArtefactoEsperadoId} ---");
            sb.AppendLine($"Nombre         : {a.NombreArtefacto}");
            sb.AppendLine($"Codigo         : {a.CodigoArtefacto ?? "N/A"}");
            sb.AppendLine($"Obligatoriedad : {a.Obligatoriedad}");
            sb.AppendLine($"Archivo        : {a.DocumentoEncontrado?.NombreArchivo ?? "N/A"}");
            sb.AppendLine($"Fuente         : {a.DocumentoEncontrado?.Fuente.ToString() ?? "N/A"}");

            if (a.SeccionesDetectadas.Count > 0)
            {
                sb.AppendLine("Secciones detectadas:");
                foreach (var s in a.SeccionesDetectadas)
                {
                    // TieneContenido = false significa que la sección existe
                    // como título pero no tiene contenido debajo.
                    var estado = s.TieneContenido ? "con contenido" : "VACIA";
                    sb.AppendLine($"  - {s.Titulo}: {estado}");
                }
            }
            else
            {
                sb.AppendLine("Secciones: no aplica template para este tipo de artefacto");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
